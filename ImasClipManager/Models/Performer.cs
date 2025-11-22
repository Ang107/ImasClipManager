using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImasClipManager.Models
{
    public class Performer
    {
        [Key]
        public int Id { get; set; }

        // 表示名そのもの (例: "天海 春香(cv: 中村 繪里子)")
        public string Name { get; set; } = string.Empty;

        // 読み (例: "あまみはるか なかむらえりこ")
        public string Yomi { get; set; } = string.Empty;

        // この出演者の形式 (絞り込みやソートに使用)
        public LiveType LiveType { get; set; } = LiveType.Seiyuu;

        // ブランド (絞り込みに使用)
        public BrandType Brand { get; set; } = BrandType.None;

        // Clipとのリレーション (シンプルに戻ります)
        public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();
    }
}