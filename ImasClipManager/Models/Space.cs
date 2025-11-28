using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImasClipManager.Models
{
    // ObservableObjectを継承させる
    public partial class Space : ObservableObject
    {
        [Key]
        public int Id { get; set; }

        // 変更通知が必要なため ObservableProperty に変更
        [ObservableProperty]
        private string _name = string.Empty;

        // Clipとのリレーション
        public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();

        public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();

        // --- UI制御用プロパティ (DBには保存しない) ---

        [NotMapped]
        [ObservableProperty]
        private bool _isEditing;

        // 編集開始前の名前を保持（キャンセル用）
        [NotMapped]
        public string OriginalName { get; set; } = string.Empty;

        // 新規作成中のアイテムかどうか
        [NotMapped]
        public bool IsNew { get; set; } = false;
    }
}