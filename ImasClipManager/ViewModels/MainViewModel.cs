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
                clip.PlayCount++;
                db.Clips.Update(clip);
                db.SaveChanges();
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
                // Performersの重複登録を防ぐため、既存のPerformerがあればAttachする処理が必要だが、
                // 現状は簡易的に追加のみとする（詳細実装時に修正推奨）
                db.Clips.Add(clip);
                db.SaveChanges();
            }
        }

        private void UpdateClipInDb(Clip clip)
        {
            using (var db = new AppDbContext())
            {
                db.Clips.Update(clip);
                db.SaveChanges();
            }
        }
    }
}