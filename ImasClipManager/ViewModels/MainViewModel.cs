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
using Microsoft.EntityFrameworkCore; // Include用
using Microsoft.Win32; // OpenFileDialog, SaveFileDialog用
using ImasClipManager.Services; // 追加
using System.Text; // 追加

namespace ImasClipManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // --- クリップ関連 ---
        public ObservableCollection<Clip> Clips { get; set; } = new ObservableCollection<Clip>();
        private ICollectionView _clipsView;

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
                    LoadClips();
                }
            }
        }

        // --- 検索 ---
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _clipsView.Refresh();
                }
            }
        }

        public MainViewModel()
        {
            // 初期化時にスペース一覧をロード
            LoadSpaces();

            // クリップのフィルタ設定
            _clipsView = CollectionViewSource.GetDefaultView(Clips);
            _clipsView.Filter = FilterClips;
        }

        // --- データ読み込みロジック ---

        public void LoadSpaces()
        {
            using (var db = new AppDbContext())
            {
                //db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                var spaceList = db.Spaces.OrderBy(s => s.Id).ToList();

                // スペースが1つもなければデフォルトを作成
                if (!spaceList.Any())
                {
                    var defaultSpace = new Space { Name = "デフォルト" };
                    db.Spaces.Add(defaultSpace);
                    db.SaveChanges();
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

        public void LoadClips()
        {
            Clips.Clear();
            if (SelectedSpace == null) return;

            using (var db = new AppDbContext())
            {
                // 選択中のスペースIDでフィルタリング + PerformersをIncludeして即時ロード
                var list = db.Clips
                             .Include(c => c.Performers)
                             .Where(c => c.SpaceId == SelectedSpace.Id)
                             .OrderByDescending(c => c.CreatedAt)
                             .ToList();

                foreach (var item in list)
                {
                    Clips.Add(item);
                }
            }
            _clipsView?.Refresh();
        }

        private bool FilterClips(object item)
        {
            if (item is not Clip clip) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var keywords = SearchText.Split(new[] { ' ', '　' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyword in keywords)
            {
                bool match = (clip.SongTitle?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (clip.ConcertName?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (clip.FilePath?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (clip.Remarks?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                             // ブランド名検索
                             (clip.Brands.ToString().Contains(keyword, System.StringComparison.OrdinalIgnoreCase));

                if (!match) return false;
            }
            return true;
        }

        // --- クリップ操作コマンド ---

        [RelayCommand]
        public void AddClip()
        {
            if (SelectedSpace == null)
            {
                MessageBox.Show("スペースを選択してください。");
                return;
            }

            var window = new ClipEditorWindow(null, EditorMode.Add);
            if (window.ShowDialog() == true)
            {
                // 選択中のスペースIDを強制セット
                window.ClipData.SpaceId = SelectedSpace.Id;
                SaveClipToDb(window.ClipData);
                LoadClips();
            }
        }

        [RelayCommand]
        public void EditClip(Clip clip)
        {
            if (clip == null) return;
            var window = new ClipEditorWindow(clip, EditorMode.Edit);
            if (window.ShowDialog() == true)
            {
                UpdateClipInDb(window.ClipData);
                LoadClips();
            }
        }

        [RelayCommand]
        public void DeleteClip(Clip clip)
        {
            if (clip == null) return;
            if (MessageBox.Show("クリップを削除しますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    var target = db.Clips.Find(clip.Id);
                    if (target != null)
                    {
                        db.Clips.Remove(target);
                        db.SaveChanges();
                    }
                }
                LoadClips();
            }
        }

        [RelayCommand]
        public void ShowClipDetail(Clip clip)
        {
            if (clip == null) return;
            new ClipEditorWindow(clip, EditorMode.Detail).ShowDialog();
        }

        [RelayCommand]
        public void PlayClip(Clip clip)
        {
            if (clip == null) return;

            using (var db = new AppDbContext())
            {
                var target = db.Clips.Find(clip.Id);
                if (target != null)
                {
                    target.PlayCount++;
                    db.SaveChanges(); // Performers は読み込んでいないので影響しない

                    // 画面表示用のオブジェクトも同期させておく
                    clip.PlayCount = target.PlayCount;
                }
            }

            _clipsView?.Refresh();

            var playerWindow = new VideoPlayerWindow(clip);
            playerWindow.Show();
        }

        // --- スペース操作コマンド ---

        [RelayCommand]
        public void AddSpace()
        {
            var input = new InputWindow("新しいスペース名:");
            if (input.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.InputText))
            {
                using (var db = new AppDbContext())
                {
                    var newSpace = new Space { Name = input.InputText.Trim() };
                    db.Spaces.Add(newSpace);
                    db.SaveChanges();
                }
                LoadSpaces(); // リロードして反映
                // 追加したスペースを選択状態にする
                SelectedSpace = Spaces.LastOrDefault();
            }
        }

        [RelayCommand]
        public void EditSpace(Space space)
        {
            if (space == null) return;

            var input = new InputWindow("スペース名を変更:", space.Name);
            if (input.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.InputText))
            {
                using (var db = new AppDbContext())
                {
                    var target = db.Spaces.Find(space.Id);
                    if (target != null)
                    {
                        target.Name = input.InputText.Trim();
                        db.SaveChanges();
                    }
                }
                LoadSpaces();
            }
        }

        [RelayCommand]
        public void DeleteSpace(Space space)
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
                using (var db = new AppDbContext())
                {
                    var target = db.Spaces.Find(space.Id);
                    if (target != null)
                    {
                        db.Spaces.Remove(target); // Cascade Delete設定によりClipsも消える
                        db.SaveChanges();
                    }
                }
                LoadSpaces();
            }
        }

        // --- DBヘルパー ---

        private void SaveClipToDb(Clip clip)
        {
            using (var db = new AppDbContext())
            {
                // ★重要: 出演者データは既にDBに存在するため、
                // "新規追加(Added)"扱いにならないように、明示的にアタッチ(Attach)します。
                // これを行わないと、既存の出演者をもう一度INSERTしようとしてエラーになります。
                if (clip.Performers != null)
                {
                    foreach (var p in clip.Performers)
                    {
                        db.Attach(p); // これで「変更なし(Unchanged)」の状態として認識されます
                    }
                }

                // Spaceが無ければ作る（簡易対策）
                if (!db.Spaces.Any())
                {
                    db.Spaces.Add(new Space { Name = "Default" });
                    db.SaveChanges();
                }

                // スペースIDの紐づけ（念のため）
                if (clip.SpaceId == 0 && SelectedSpace != null)
                {
                    clip.SpaceId = SelectedSpace.Id;
                }

                db.Clips.Add(clip);
                db.SaveChanges();
            }
        }

        private void UpdateClipInDb(Clip clip)
        {
            using (var db = new AppDbContext())
            {
                // 編集の場合、リレーション（多対多）を安全に更新するために
                // 一度DBから現在の状態を読み込んでから、差分を適用する方法が最も確実です。

                var existingClip = db.Clips
                                     .Include(c => c.Performers) // 現在の出演者紐づけも含めてロード
                                     .FirstOrDefault(c => c.Id == clip.Id);

                if (existingClip != null)
                {
                    // 1. クリップ本体の値（タイトルや日付など）をコピーして更新
                    db.Entry(existingClip).CurrentValues.SetValues(clip);

                    // 2. 出演者リストの更新
                    // 一旦紐づけをクリア
                    existingClip.Performers.Clear();

                    // 画面で選択された出演者を再登録
                    foreach (var p in clip.Performers)
                    {
                        // ここでも「既存のデータだよ」と教えるためにAttachを使いたいのですが、
                        // 同じコンテキスト内ですでに追跡されているか確認してから行います。
                        var trackedPerformer = db.Performers.Local.FirstOrDefault(x => x.Id == p.Id);

                        if (trackedPerformer != null)
                        {
                            // 既に追跡中ならそれを使う
                            existingClip.Performers.Add(trackedPerformer);
                        }
                        else
                        {
                            // 追跡されていないならAttachして追加
                            db.Attach(p);
                            existingClip.Performers.Add(p);
                        }
                    }

                    db.SaveChanges();
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
                    using (var db = new AppDbContext())
                    {
                        var list = db.Performers.OrderBy(p => p.Id).ToList();
                        var service = new CsvDataService();
                        service.ExportPerformers(dialog.FileName, list);
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
                    var records = service.ImportPerformers(dialog.FileName);

                    using (var db = new AppDbContext())
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
    }
}