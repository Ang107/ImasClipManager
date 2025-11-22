using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImasClipManager.Models
{
    [Flags]
    public enum BrandType
    {
        None = 0,
        Imas = 1 << 0,   // 765プロオールスターズ
        DS = 1 << 1,         // ディアリースターズ
        Cinderella = 1 << 2, // シンデレラガールズ
        Million = 1 << 3,    // ミリオンライブ！
        SideM = 1 << 4,      // SideM
        Shiny = 1 << 5,      // シャイニーカラーズ
        Valiv = 1 << 6,      // ヴイアライヴ
        Gakuen = 1 << 7,     // 学園アイドルマスター
        Goudou = 1 << 8,     // 合同ライブ
        Other = 1 << 9       // その他
    }

    public enum LiveType
    {
        Seiyuu, // 声優ライブ
        MR,     // MRライブ
        Other   // その他
    }

    public class Clip
    {
        [Key]
        public int Id { get; set; }

        public int SpaceId { get; set; }

        [Required(ErrorMessage = "ファイルパスは必須です")]
        public string FilePath { get; set; } = string.Empty;

        // DBにはミリ秒で保存
        public long StartTimeMs { get; set; } = 0;
        public long? EndTimeMs { get; set; } // nullなら最後まで

        // 時間表示・入力用プロパティ
        [NotMapped]
        public string StartTimeStr
        {
            get => TimeSpan.FromMilliseconds(StartTimeMs).ToString(@"hh\:mm\:ss");
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    StartTimeMs = 0;
                    return;
                }

                // ★修正: コロンが1つの場合(xx:yy)は mm:ss (分:秒) として解釈する
                var parts = value.Split(':');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], out double mm) && double.TryParse(parts[1], out double ss))
                    {
                        StartTimeMs = (long)(TimeSpan.FromMinutes(mm) + TimeSpan.FromSeconds(ss)).TotalMilliseconds;
                        return;
                    }
                }

                // それ以外(hh:mm:ssなど)は標準パーサーに任せる
                if (TimeSpan.TryParse(value, out var ts))
                {
                    StartTimeMs = (long)ts.TotalMilliseconds;
                }
            }
        }

        [NotMapped]
        public string EndTimeStr
        {
            get => EndTimeMs.HasValue ? TimeSpan.FromMilliseconds(EndTimeMs.Value).ToString(@"hh\:mm\:ss") : "";
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    EndTimeMs = null;
                    return;
                }

                // ★修正: コロンが1つの場合(xx:yy)は mm:ss (分:秒) として解釈する
                var parts = value.Split(':');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], out double mm) && double.TryParse(parts[1], out double ss))
                    {
                        EndTimeMs = (long)(TimeSpan.FromMinutes(mm) + TimeSpan.FromSeconds(ss)).TotalMilliseconds;
                        return;
                    }
                }

                if (TimeSpan.TryParse(value, out var ts))
                {
                    EndTimeMs = (long)ts.TotalMilliseconds;
                }
            }
        }

        public string ConcertName { get; set; } = string.Empty;

        public BrandType Brands { get; set; } = BrandType.None;

        public LiveType LiveType { get; set; } = LiveType.Seiyuu;
        public DateTime ConcertDate { get; set; } = DateTime.Today;

        public string SongTitle { get; set; } = string.Empty;

        public string ThumbnailPath { get; set; } = string.Empty;
        public bool IsAutoThumbnail { get; set; } = true;
        public int PlayCount { get; set; } = 0;
        public string Lyrics { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public virtual ICollection<Performer> Performers { get; set; } = new List<Performer>();
    }
}