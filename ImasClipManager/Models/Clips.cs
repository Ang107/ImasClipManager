using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ImasClipManager.Helpers;

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

    public partial class Clip : ObservableObject
    {
        [Key]
        public int Id { get; set; }

        public int SpaceId { get; set; }

        [Required(ErrorMessage = "ファイルパスは必須です")]
        public string FilePath { get; set; } = string.Empty;

        // ... (時間関連はそのまま) ...
        public long StartTimeMs { get; set; } = 0;
        public long? EndTimeMs { get; set; }

        [NotMapped]
        public string StartTimeStr
        {
            get => TimeSpan.FromMilliseconds(StartTimeMs).ToString(@"hh\:mm\:ss");
            set
            {
                // 共通ロジックを使用
                if (TimeHelper.TryParseTime(value, out long ms))
                {
                    StartTimeMs = ms;
                }
                // ※パース失敗時は値を更新しない、あるいは0にするなど仕様に合わせて調整
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
                }
                else if (TimeHelper.TryParseTime(value, out long ms))
                {
                    EndTimeMs = ms;
                }
            }
        }

        public string ConcertName { get; set; } = string.Empty;
        public BrandType Brands { get; set; } = BrandType.None;
        public LiveType LiveType { get; set; } = LiveType.Seiyuu;
        public DateTime ConcertDate { get; set; } = DateTime.Today;
        public string SongTitle { get; set; } = string.Empty;

        // ★変更: UIに通知したいプロパティを ObservableProperty 形式に変更

        private string _thumbnailPath = string.Empty;
        public string ThumbnailPath
        {
            get => _thumbnailPath;
            set => SetProperty(ref _thumbnailPath, value);
        }

        private bool _isAutoThumbnail = true;
        public bool IsAutoThumbnail
        {
            get => _isAutoThumbnail;
            set => SetProperty(ref _isAutoThumbnail, value);
        }

        private int _playCount = 0;
        public int PlayCount
        {
            get => _playCount;
            set => SetProperty(ref _playCount, value);
        }

        // ... (残りのプロパティ) ...
        public string Lyrics { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Performer> Performers { get; set; } = new List<Performer>();
    }
}