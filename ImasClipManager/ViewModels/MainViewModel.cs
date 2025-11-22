using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows; // MessageBox用
using System.Windows.Data; // ICollectionView用
using System.ComponentModel; // ICollectionView用
using ImasClipManager.Models;
using ImasClipManager.Data;
using ImasClipManager.Views;

namespace ImasClipManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // データを保持するリスト
        public ObservableCollection<Clip> Clips { get; set; } = new ObservableCollection<Clip>();

        // 検索フィルタ用のビュー
        private ICollectionView _clipsView;

        // 検索テキスト
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _clipsView.Refresh(); // 文字入力のたびにフィルタ実行
                }
            }
        }

        public MainViewModel()
        {
            LoadData();

            // フィルタ機能の初期化
            _clipsView = CollectionViewSource.GetDefaultView(Clips);
            _clipsView.Filter = FilterClips;
        }

        // 検索ロジック
        private bool FilterClips(object item)
        {
            if (item is not Clip clip) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var keywords = SearchText.Split(new[] { ' ', '　' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyword in keywords)
            {
                // 曲名、公演名、ファイルパス、備考などでAND検索
                bool match = (clip.SongTitle?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (clip.ConcertName?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (clip.FilePath?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (clip.Remarks?.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ?? false);

                if (!match) return false;
            }
            return true;
        }

        // データ読み込み
        public void LoadData()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                var list = db.Clips.OrderByDescending(c => c.CreatedAt).ToList();

                Clips.Clear();
                foreach (var item in list)
                {
                    Clips.Add(item);
                }
            }
            _clipsView?.Refresh();
        }

        // --- コマンド実装 ---

        // 新規追加
        [RelayCommand]
        public void AddClip()
        {
            // Addモードで開く
            var window = new ClipEditorWindow(null, EditorMode.Add);
            if (window.ShowDialog() == true)
            {
                SaveClipToDb(window.ClipData);
                LoadData();
            }
        }

        // ★追加: 編集
        [RelayCommand]
        public void EditClip(Clip clip)
        {
            if (clip == null) return;

            // Editモードで開く
            var window = new ClipEditorWindow(clip, EditorMode.Edit);
            if (window.ShowDialog() == true)
            {
                UpdateClipInDb(window.ClipData);
                LoadData();
            }
        }

        // ★追加: 削除
        [RelayCommand]
        public void DeleteClip(Clip clip)
        {
            if (clip == null) return;

            // 確認メッセージ
            var result = MessageBox.Show($"クリップを削除してもよろしいですか？",
                                         "削除確認",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    // 削除対象を再度取得して削除
                    var target = db.Clips.Find(clip.Id);
                    if (target != null)
                    {
                        db.Clips.Remove(target);
                        db.SaveChanges();
                    }
                }
                LoadData();
            }
        }

        // ★追加: 詳細表示
        [RelayCommand]
        public void ShowClipDetail(Clip clip)
        {
            if (clip == null) return;

            // Detailモードで開く (保存処理は不要なのでDialogResultは見ない)
            var window = new ClipEditorWindow(clip, EditorMode.Detail);
            window.ShowDialog();
        }

        // 再生 (ダブルクリック時など)
        [RelayCommand]
        public void PlayClip(Clip clip)
        {
            if (clip == null) return;
            var playerWindow = new VideoPlayerWindow(clip);
            playerWindow.Show();
        }

        // --- DBヘルパーメソッド ---

        private void SaveClipToDb(Clip clip)
        {
            using (var db = new AppDbContext())
            {
                // Spaceが無ければ作る（簡易対策）
                if (!db.Spaces.Any())
                {
                    db.Spaces.Add(new Space { Name = "Default" });
                    db.SaveChanges();
                }
                clip.SpaceId = db.Spaces.First().Id;

                db.Clips.Add(clip);
                db.SaveChanges();
            }
        }

        private void UpdateClipInDb(Clip clip)
        {
            using (var db = new AppDbContext())
            {
                // IDの一致するレコードを上書き更新
                db.Clips.Update(clip);
                db.SaveChanges();
            }
        }
    }
}