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
using System.Threading.Tasks;

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
                    _ = LoadClipsAsync();
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
            _ = InitializeAsync();
            // クリップのフィルタ設定
            _clipsView = CollectionViewSource.GetDefaultView(Clips);
            _clipsView.Filter = FilterClips;
        }

        private async Task InitializeAsync()
        {
            await LoadSpacesAsync();
        }

        // --- データ読み込みロジック ---
        public async Task LoadSpacesAsync()
        {
            using (var db = new AppDbContext())
            {
                //db.Database.EnsureDeleted();
                await db.Database.EnsureCreatedAsync();
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

            using (var db = new AppDbContext())
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
        public async Task AddClip()
        {
            if (SelectedSpace == null)
            {
                MessageBox.Show("スペースを選択してください。");
                return;
            }

            // ViewModelを作成してWindowに渡す
            var vm = new ClipEditorViewModel(null, EditorMode.Add, SelectedSpace.Id);
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

            var vm = new ClipEditorViewModel(clip, EditorMode.Edit, clip.SpaceId);
            var window = new ClipEditorWindow(vm);

            if (window.ShowDialog() == true)
            {
                await UpdateClipInDbAsync(window.ClipData);
                await LoadClipsAsync();
            }
        }

        [RelayCommand]
        public void ShowClipDetail(Clip clip)
        {
            if (clip == null) return;

            var vm = new ClipEditorViewModel(clip, EditorMode.Detail, clip.SpaceId);
            var window = new ClipEditorWindow(vm);
            window.ShowDialog();
        }


        [RelayCommand]
        public async Task PlayClip(Clip clip)
        {
            if (clip == null) return;

            using (var db = new AppDbContext())
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

        // --- スペース操作コマンド ---

        [RelayCommand]
        public async Task AddSpace()
        {
            var input = new InputWindow("新しいスペース名:");

            // バリデーションルールを設定 (空文字チェック)
            input.Validator = (text) =>
                string.IsNullOrWhiteSpace(text) ? "スペース名を入力してください。" : null;

            // ShowDialog() が true で返ってくるのは、バリデーションを通過してOKが押された場合のみ
            if (input.ShowDialog() == true)
            {
                using (var db = new AppDbContext())
                {
                    var newSpace = new Space { Name = input.InputText.Trim() };
                    db.Spaces.Add(newSpace);
                    await db.SaveChangesAsync();
                }
                await LoadSpacesAsync();
                SelectedSpace = Spaces.LastOrDefault();
            }
        }

        [RelayCommand]
        public async Task EditSpace(Space space)
        {
            if (space == null) return;

            var input = new InputWindow("スペース名を変更:", space.Name);

            // バリデーションルールを設定
            input.Validator = (text) =>
                string.IsNullOrWhiteSpace(text) ? "スペース名は空にできません。" : null;

            if (input.ShowDialog() == true)
            {
                using (var db = new AppDbContext())
                {
                    var target = await db.Spaces.FindAsync(space.Id);
                    if (target != null)
                    {
                        target.Name = input.InputText.Trim();
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
            using (var db = new AppDbContext())
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
            using (var db = new AppDbContext())
            {
                // FirstOrDefault -> await FirstOrDefaultAsync
                var existingClip = await db.Clips
                                     .Include(c => c.Performers)
                                     .FirstOrDefaultAsync(c => c.Id == clip.Id); // ★ここを非同期に

                if (existingClip != null)
                {
                    db.Entry(existingClip).CurrentValues.SetValues(clip);
                    existingClip.Performers.Clear();

                    foreach (var p in clip.Performers)
                    {
                        var trackedPerformer = db.Performers.Local.FirstOrDefault(x => x.Id == p.Id);
                        if (trackedPerformer != null)
                        {
                            existingClip.Performers.Add(trackedPerformer);
                        }
                        else
                        {
                            db.Attach(p);
                            existingClip.Performers.Add(p);
                        }
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