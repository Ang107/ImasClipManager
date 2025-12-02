using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using ImasClipManager.Models;
using ImasClipManager.Data;
using ImasClipManager.Views;
using ImasClipManager.Helpers;
using Microsoft.EntityFrameworkCore; // Include用
using Microsoft.Win32; // OpenFileDialog, SaveFileDialog用
using ImasClipManager.Services; // 追加
using System.Text; // 追加
using System.Threading;
using System.Threading.Tasks;

namespace ImasClipManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly Func<AppDbContext> _dbFactory; // DB作成ファクトリ
        private readonly ThumbnailService _thumbnailService;
        private readonly CsvDataService _csvDataService;
        // --- クリップ関連 ---
        public ObservableCollection<Clip> Clips { get; set; } = new ObservableCollection<Clip>();
        private ICollectionView _clipsView;

        // ★追加: スペースペインの幅（初期値 250）
        [ObservableProperty]
        private double _spacePaneWidth = 250.0;

        // ★追加: 閉じる直前の幅を記録する変数
        private double _lastSpacePaneWidth = 250.0;

        // ★追加: 閾値（これより小さい幅で保存されていたらデフォルトに戻す）
        private const double RestoreThreshold = 100.0;
        private const double DefaultWidth = 250.0;
        private const double CollapsedWidth = 50.0;
        // ★追加: ループ防止・ドラッグ判定用のフラグ
        private bool _isSyncingMode = false;

        // ★修正: 幅が変更されたときの処理
        partial void OnSpacePaneWidthChanged(double value)
        {
            // ご要望により「閾値以下なら50にする」処理は削除しました。
            // 50未満にならない制限はViewのMinWidth="50"で担保されます。

            // ここでは、幅の変更に合わせて「開閉フラグ」だけを同期させます。
            // ただし、ここでの変更がトリガーとなって OnIsSpacePaneVisibleChanged が呼ばれ、
            // 幅が勝手に書き換わるのを防ぐため、フラグ(_isSyncingMode)を立てます。
            _isSyncingMode = true;
            try
            {
                // 幅が最小幅(50)になったら「閉」状態
                // ※計算誤差を考慮して少し余裕を持たせて判定しても良いですが、
                // ViewのMinWidthと一致していれば == でもおおよそ機能します
                if (value <= CollapsedWidth)
                {
                    if (IsSpacePaneVisible)
                    {
                        IsSpacePaneVisible = false;
                    }
                }
                // 幅が50を超えていたら「開」状態
                else
                {
                    if (!IsSpacePaneVisible)
                    {
                        IsSpacePaneVisible = true;
                    }
                }
            }
            finally
            {
                _isSyncingMode = false;
            }
        }

        // ★修正: 開閉フラグ変更時の処理
        partial void OnIsSpacePaneVisibleChanged(bool value)
        {
            // ドラッグ操作によって幅が変更され、その結果としてここが呼ばれた場合は
            // 幅の復元処理（強制リサイズ）を行わないようにします。
            // これにより「少し広げただけで250に戻される」問題が解消します。
            if (_isSyncingMode) return;

            if (value) // ボタン操作で開くとき
            {
                // 記録された幅が閾値(100)以下ならデフォルト(250)で開く
                if (_lastSpacePaneWidth <= RestoreThreshold)
                {
                    SpacePaneWidth = DefaultWidth;
                }
                else
                {
                    SpacePaneWidth = _lastSpacePaneWidth;
                }
            }
            else // ボタン操作で閉じるとき
            {
                // 現在の幅が50より大きい場合のみ記録する
                if (SpacePaneWidth > CollapsedWidth)
                {
                    _lastSpacePaneWidth = SpacePaneWidth;
                }

                // 閉じるので幅を50にする
                if (SpacePaneWidth > CollapsedWidth)
                {
                    SpacePaneWidth = CollapsedWidth;
                }
            }
        }

        // --- スペース関連 ---
        public ObservableCollection<Space> Spaces { get; set; } = new ObservableCollection<Space>();

        private Space? _selectedSpace;
        public Space? SelectedSpace
        {
            get => _selectedSpace;
            set
            {
                if (SetProperty(ref _selectedSpace, value))
                {
                    // スペースが切り替わったらクリップを再ロード
                    _ = LoadClipsAsync();
                }
            }
        }

        // --- 検索 ---
        private CancellationTokenSource? _debounceCts;
        private string _searchText = string.Empty;
        private Func<Clip, bool>? _currentFilterPredicate;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _currentFilterPredicate = SearchQueryParser.Parse(value, Settings);
                    SearchWithDebounce();
                }
            }
        }

        // デバウンス処理の実体
        private async void SearchWithDebounce()
        {
            try
            {
                // 前回の待機処理があればキャンセル
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;

                // 300ミリ秒待機 (入力中はここでキャンセルされるため後続処理が走らない)
                await Task.Delay(300, token);

                // UIスレッドでフィルタを更新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _clipsView?.Refresh();
                });
            }
            catch (TaskCanceledException)
            {
                // キャンセルされた場合は何もしない
            }
        }
        public SettingsViewModel Settings { get; }
        public MainViewModel(Func<AppDbContext> dbFactory, ThumbnailService thumbnailService, CsvDataService csvDataService)
        {
            _dbFactory = dbFactory;
            _thumbnailService = thumbnailService;
            _csvDataService = csvDataService;
            Settings = new SettingsViewModel(_dbFactory);
            // ★追加: 設定(検索対象のON/OFF)が変わったときもフィルタを再実行して即座に反映させる
            Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null && e.PropertyName.StartsWith("Search_"))
                {
                    _clipsView?.Refresh();
                }
            };
            // クリップのフィルタ設定
            _clipsView = CollectionViewSource.GetDefaultView(Clips);
            _clipsView.Filter = FilterClips;
            _ = InitializeAsync();

        }
        private bool FilterClips(object item)
        {
            if (item is not Clip clip) return false;

            // パーサーで生成した条件で判定
            if (_currentFilterPredicate != null)
            {
                return _currentFilterPredicate(clip);
            }
            return true;
        }

        // ★追加: 高度な検索ウィンドウを開くコマンド
        [RelayCommand]
        public void OpenAdvancedSearch()
        {
            var vm = new AdvancedSearchViewModel(this);
            var win = new Views.AdvancedSearchWindow();
            win.DataContext = vm;
            win.Owner = Application.Current.MainWindow;
            win.Show(); // モードレスで開く（メイン画面の変化が見えるように）
        }


        private async Task InitializeAsync()
        {
            await LoadSpacesAsync();
            await Settings.InitializeAsync(); // 設定ロード
        }

        // --- データ読み込みロジック ---
        public async Task LoadSpacesAsync()
        {
            using (var db = _dbFactory())
            {
                await db.Database.MigrateAsync();
                var spaceList = await db.Spaces.OrderBy(s => s.Id).ToListAsync();

                // スペースが1つもなければデフォルトを作成
                if (!spaceList.Any())
                {
                    var defaultSpace = new Space { Name = "デフォルト" };
                    db.Spaces.Add(defaultSpace);
                    await db.SaveChangesAsync();
                    spaceList.Add(defaultSpace);
                }

                Spaces.Clear();
                foreach (var s in spaceList) Spaces.Add(s);

                // 先頭を選択状態にする
                if (SelectedSpace == null && Spaces.Any())
                {
                    SelectedSpace = Spaces.First();
                }
                else if (SelectedSpace != null)
                {
                    // IDで再選択（インスタンスが変わるため）
                    SelectedSpace = Spaces.FirstOrDefault(s => s.Id == SelectedSpace.Id) ?? Spaces.First();
                }
            }
        }

        public async Task LoadClipsAsync()
        {
            Clips.Clear();
            if (SelectedSpace == null) return;

            using (var db = _dbFactory())
            {
                // 選択中のスペースIDでフィルタリング + PerformersをIncludeして即時ロード
                var list = await db.Clips
                             .Include(c => c.Performers)
                             .Where(c => c.SpaceId == SelectedSpace.Id)
                             .OrderByDescending(c => c.CreatedAt)
                             .ToListAsync();

                foreach (var item in list)
                {
                    Clips.Add(item);
                }
            }
            _clipsView?.Refresh();
        }

        // --- クリップ操作コマンド ---

        [RelayCommand]
        public async Task AddClip()
        {
            if (SelectedSpace == null)
            {
                MessageBox.Show("スペースを選択してください。");
                return;
            }

            // ViewModelを作成してWindowに渡す
            var vm = new ClipEditorViewModel(null, EditorMode.Add, SelectedSpace.Id, _thumbnailService, Settings);
            var window = new ClipEditorWindow(vm);

            if (window.ShowDialog() == true)
            {
                window.ClipData.SpaceId = SelectedSpace.Id;
                await SaveClipToDbAsync(window.ClipData);
                await LoadClipsAsync();
            }
        }

        [RelayCommand]
        public async Task EditClip(Clip clip)
        {
            if (clip == null) return;

            var vm = new ClipEditorViewModel(clip, EditorMode.Edit, clip.SpaceId, _thumbnailService, Settings);
            var window = new ClipEditorWindow(vm);

            if (window.ShowDialog() == true)
            {
                await UpdateClipInDbAsync(window.ClipData);
                await LoadClipsAsync();
            }
        }

        [RelayCommand]
        public async Task DeleteClip(Clip clip)
        {
            if (clip == null) return;
            if (MessageBox.Show("クリップを削除しますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = _dbFactory())
                {
                    var target = await db.Clips.FindAsync(clip.Id);
                    if (target != null)
                    {
                        if (!string.IsNullOrEmpty(target.ThumbnailPath))
                        {
                            _thumbnailService.DeleteFile(target.ThumbnailPath);
                        }
                        db.Clips.Remove(target);
                        await db.SaveChangesAsync();
                    }
                }
                await LoadClipsAsync();
            }
        }

        [RelayCommand]
        public void ShowClipDetail(Clip clip)
        {
            if (clip == null) return;

            var vm = new ClipEditorViewModel(clip, EditorMode.Detail, clip.SpaceId, _thumbnailService, Settings);
            var window = new ClipEditorWindow(vm);
            window.ShowDialog();
        }


        [RelayCommand]
        public async Task PlayClip(Clip clip)
        {
            if (clip == null) return;

            using (var db = _dbFactory())
            {
                var target = await db.Clips.FindAsync(clip.Id);
                if (target != null)
                {
                    target.PlayCount++;
                    await db.SaveChangesAsync(); // Performers は読み込んでいないので影響しない

                    // 画面表示用のオブジェクトも同期させておく
                    clip.PlayCount = target.PlayCount;
                }
            }

            _clipsView?.Refresh();

            var playerWindow = new VideoPlayerWindow(clip);
            playerWindow.Show();
        }


        // --- スペース操作コマンド (刷新) ---

        // 1. スペース追加 (＋ボタン)
        [RelayCommand]
        public void AddSpace()
        {
            var newSpace = new Space
            {
                Name = "新規スペース", // 初期値
                IsEditing = true,
                IsNew = true,
                OriginalName = "" // 新規なので空
            };

            Spaces.Add(newSpace);
            SelectedSpace = newSpace;

            // ※View側でCollectionChangedを検知してスクロールさせる
        }

        // 2. スペース名変更 (ダブルクリック/メニュー)
        [RelayCommand]
        public void EditSpace(Space space)
        {
            if (space == null) return;

            // 編集モードに入る前に現在の名前を退避
            space.OriginalName = space.Name;
            space.IsNew = false;
            space.IsEditing = true;
        }

        // 3. エンターキーでの確定試行
        [RelayCommand]
        public async Task CommitSpaceEdit(Space space)
        {
            if (space == null) return;

            // バリデーション: 空文字チェック
            if (string.IsNullOrWhiteSpace(space.Name))
            {
                MessageBox.Show("スペース名を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                // 編集継続 (IsEditing = true のまま)
                return;
            }

            // 正常系: 保存
            await SaveSpaceAsync(space);
        }

        // 4. フォーカスが外れたとき (編集終了またはキャンセル)
        [RelayCommand]
        public async Task EndSpaceEdit(Space space)
        {
            if (space == null || !space.IsEditing) return;

            // 空文字の場合の処理
            if (string.IsNullOrWhiteSpace(space.Name))
            {
                if (space.IsNew)
                {
                    // 新規作成で空文字なら削除 (作成キャンセル)
                    Spaces.Remove(space);
                }
                else
                {
                    // 既存なら元の名前に戻す
                    space.Name = space.OriginalName;
                    space.IsEditing = false;
                }
                return;
            }

            // 入力がある場合は保存して終了
            await SaveSpaceAsync(space);
        }

        // 保存の共通処理
        private async Task SaveSpaceAsync(Space space)
        {
            using (var db = _dbFactory())
            {
                if (space.IsNew)
                {
                    space.Id = 0; // ID自動採番
                    db.Spaces.Add(space);
                }
                else
                {
                    // 既存更新
                    var target = await db.Spaces.FindAsync(space.Id);
                    if (target != null)
                    {
                        target.Name = space.Name;
                    }
                }
                await db.SaveChangesAsync();
            }

            // フラグをリセット
            space.IsNew = false;
            space.IsEditing = false;
            space.OriginalName = string.Empty;

            // リスト再読み込みはせず、IDだけ同期できればベストだが、
            // 簡易的に再ロードするか、そのまま利用する
            if (space.Id == 0) await LoadSpacesAsync();
        }

        [RelayCommand]
        public void CancelSpaceEdit(Space space)
        {
            if (space == null || !space.IsEditing) return;

            if (space.IsNew)
            {
                // 新規作成中ならリストから削除
                Spaces.Remove(space);
            }
            else
            {
                // 既存なら元の名前に戻して編集モード終了
                space.Name = space.OriginalName;
                space.IsEditing = false;
            }
            // ここでは SaveSpaceAsync を呼ばない！
        }

        [RelayCommand]
        public async Task DeleteSpace(Space space)
        {
            if (space == null) return;

            // 最後の1つは削除させないなどの制御が必要ならここに入れる
            if (Spaces.Count <= 1)
            {
                MessageBox.Show("最後のスペースは削除できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show($"スペース「{space.Name}」を削除しますか？\n※含まれるクリップもすべて削除されます。",
                                      "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                using (var db = _dbFactory())
                {
                    var target = await db.Spaces.FindAsync(space.Id);
                    if (target != null)
                    {
                        db.Spaces.Remove(target); // Cascade Delete設定によりClipsも消える
                        await db.SaveChangesAsync();
                    }
                }
                await LoadSpacesAsync();
            }
        }




        [ObservableProperty]
        private bool _isSpacePaneVisible = true;

        // --- DBヘルパー ---

        private async Task SaveClipToDbAsync(Clip clip)
        {
            using (var db = _dbFactory())
            {
                if (clip.Performers != null)
                {
                    foreach (var p in clip.Performers) db.Attach(p);
                }

                // Any() -> await AnyAsync(), SaveChanges() -> await SaveChangesAsync()
                if (!await db.Spaces.AnyAsync())
                {
                    db.Spaces.Add(new Space { Name = "Default" });
                    await db.SaveChangesAsync();
                }

                if (clip.SpaceId == 0 && SelectedSpace != null)
                {
                    clip.SpaceId = SelectedSpace.Id;
                }

                db.Clips.Add(clip);
                await db.SaveChangesAsync(); // ★ここを非同期に
            }
        }

        private async Task UpdateClipInDbAsync(Clip clip)
        {
            using (var db = _dbFactory())
            {
                // FirstOrDefault -> await FirstOrDefaultAsync
                var existingClip = await db.Clips
                                     .Include(c => c.Performers)
                                     .FirstOrDefaultAsync(c => c.Id == clip.Id); // ★ここを非同期に

                if (existingClip != null)
                {
                    db.Entry(existingClip).CurrentValues.SetValues(clip);

                    // 修正: Clear() -> Add() ではなく、差分更新を行う
                    // これにより、既存のリレーションが誤って削除状態のままになるのを防ぐ

                    var newPerformerIds = clip.Performers.Select(p => p.Id).ToHashSet();

                    // 1. 削除対象: 既存リストにあり、新リストにないもの
                    var toRemove = existingClip.Performers.Where(p => !newPerformerIds.Contains(p.Id)).ToList();
                    foreach (var p in toRemove)
                    {
                        existingClip.Performers.Remove(p);
                    }

                    // 2. 追加対象: 新リストにあり、既存リストにないもの
                    foreach (var p in clip.Performers)
                    {
                        // 既に存在する場合はスキップ(維持)
                        if (existingClip.Performers.Any(existing => existing.Id == p.Id)) continue;
                        db.Attach(p);
                        existingClip.Performers.Add(p);
                    }

                    await db.SaveChangesAsync(); // ★ここを非同期に
                }
            }
        }

        // --- CSV操作コマンド ---

        [RelayCommand]
        public void ExportPerformers()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv",
                FileName = "performers.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try // ★追加: エラー監視開始
                {
                    using (var db = _dbFactory())
                    {
                        var list = db.Performers.OrderBy(p => p.Id).ToList();
                        _csvDataService.ExportPerformers(dialog.FileName, list);
                    }
                    MessageBox.Show("エクスポートが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.IO.IOException) // ★追加: ファイルロックのエラーを捕捉
                {
                    MessageBox.Show("ファイルが開かれているため保存できませんでした。\nExcelなどを閉じてから再試行してください。",
                                    "保存エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (System.Exception ex) // ★追加: その他の予期せぬエラー
                {
                    MessageBox.Show($"エクスポートに失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        public void ImportPerformers()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var service = new CsvDataService();
                    var records = _csvDataService.ImportPerformers(dialog.FileName);

                    using (var db = _dbFactory())
                    {
                        int addedCount = 0;
                        int updatedCount = 0;

                        // DBの全データをメモリにロード
                        var dbPerformers = db.Performers.ToList();

                        foreach (var item in records)
                        {
                            // 名前(Display Name)が一致する既存データを探す
                            var existing = dbPerformers.FirstOrDefault(p => p.Name == item.Name);

                            if (existing != null)
                            {
                                // ★一致するなら更新 (IDは変えずに中身をCSVの値で上書き)
                                existing.Yomi = item.Yomi;
                                existing.LiveType = item.LiveType;
                                existing.Brand = item.Brand;
                                updatedCount++;
                            }
                            else
                            {
                                // ★一致しないなら新規追加
                                item.Id = 0; // IDは自動採番させる
                                db.Performers.Add(item);

                                // CSV内に同じ名前が複数行あった場合、後続の行で「更新」扱いにするためリストにも追加しておく
                                dbPerformers.Add(item);
                                addedCount++;
                            }
                        }

                        db.SaveChanges();
                        MessageBox.Show($"{records.Count} 件処理しました。\n追加: {addedCount} 件\n更新: {updatedCount} 件", "インポート完了");
                    }
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("ファイルが開かれているため読み込めませんでした。\nExcelなどを閉じてから再試行してください。",
                                    "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"インポートに失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- メンテナンスコマンド ---

        // ★追加: サムネイルのお掃除コマンド
        [RelayCommand]
        public async Task CleanUpThumbnails()
        {
            if (MessageBox.Show("使われていないサムネイル画像を検索して削除しますか？\n" +
                                "※DBに登録されていない画像ファイルが対象です。",
                                "クリーンアップ",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            int count = 0;
            using (var db = _dbFactory())
            {
                // 全クリップのサムネパスを取得
                var allPaths = await db.Clips
                                       .Where(c => c.ThumbnailPath != null && c.ThumbnailPath != "")
                                       .Select(c => c.ThumbnailPath)
                                       .ToListAsync();

                count = await _thumbnailService.CleanUpUnusedThumbnailsAsync(allPaths);
            }

            MessageBox.Show($"クリーンアップが完了しました。\n削除されたファイル: {count} 件", "完了");
        }
    }
}