using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImasClipManager.Models
{
    public partial class Playlist : ObservableObject
    {
        [Key]
        public int Id { get; set; }

        public int SpaceId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public int SortIndex { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // --- リレーション ---

        [ForeignKey(nameof(SpaceId))]
        public virtual Space? Space { get; set; }

        // 単純な多対多リレーション
        public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();
    }
}