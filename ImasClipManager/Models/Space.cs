using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImasClipManager.Models
{
    // 入れ子にできない「スペース」
    public class Space
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Clipとのリレーション
        public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();
    }
}