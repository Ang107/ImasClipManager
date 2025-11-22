using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImasClipManager.Models
{
    // 出演者マスター
    public class Performer
    {
        [Key]
        public int Id { get; set; }

        public string Brand { get; set; } = string.Empty; // 765, ML, CG...
        public string Name { get; set; } = string.Empty; // 声優名
        public string NameYomi { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty; // キャラ名
        public string CharacterYomi { get; set; } = string.Empty;

        // Clipとの多対多リレーション
        public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();
    }
}