using System.ComponentModel.DataAnnotations;

namespace ImasClipManager.Models
{
    public enum DisplayContext
    {
        Table,  // 表 (DataGrid)
        Editor,  // 編集・詳細画面
        Search  // ★追加: 検索設定
    }

    public class DisplayState
    {
        [Key]
        public int Id { get; set; }
        public DisplayContext Context { get; set; }
        public string PropertyKey { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
    }
}