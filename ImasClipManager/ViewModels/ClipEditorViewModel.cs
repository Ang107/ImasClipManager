using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImasClipManager.Helpers;
using ImasClipManager.Models;
using ImasClipManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // MessageBox用 (本来はService化推奨だが簡易的に使用)
using Xabe.FFmpeg;

namespace ImasClipManager.ViewModels
{
    public enum EditorMode
    {
        Add,    // 追加
        Edit,   // 編集
        Detail  // 詳細
    }

    public class BrandSelection
    {
        public string Name { get; set; } = string.Empty;
        public BrandType Value { get; set; }
        public bool IsSelected { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public partial class ClipEditorViewModel : ObservableObject
    {
        private readonly ThumbnailService _thumbnailService;
        public Clip ClipData { get; private set; }
        public List<BrandSelection> BrandList { get; set; }
        public EditorMode Mode { get; private set; }

        // 画面表示用プロパティ
        [ObservableProperty] private string _windowTitle = string.Empty;
        [ObservableProperty] private string _actionButtonText = string.Empty;
        [ObservableProperty] private bool _isEditable;

        // エラーメッセージ用プロパティ
        [ObservableProperty] private string _filePathErrorMessage = string.Empty;
        [ObservableProperty] private string _startTimeErrorMessage = string.Empty;
        [ObservableProperty] private string _endTimeErrorMessage = string.Empty;

        // 動画の長さキャッシュ
        private double _videoDurationMs = 0;

        // 出演者表示用
        public string PerformersDisplayText
        {
            get
            {
                if (ClipData.Performers == null || !ClipData.Performers.Any()) return "(未選択)";
                return string.Join(Environment.NewLine, ClipData.Performers.Select(p => p.Name));
            }
        }
        public SettingsViewModel Settings { get; }

        public ClipEditorViewModel(Clip? clip, EditorMode mode, int spaceId, ThumbnailService thumbnailService, SettingsViewModel settings)
        {
            _thumbnailService = thumbnailService;
            Settings = settings;
            Mode = mode;

            // モード設定
            switch (Mode)
            {
                case EditorMode.Add:
                    WindowTitle = "クリップ追加";
                    ActionButtonText = "追加";
                    IsEditable = true;
                    break;
                case EditorMode.Edit:
                    WindowTitle = "クリップ編集";
                    ActionButtonText = "保存";
                    IsEditable = true;
                    break;
                case EditorMode.Detail:
                    WindowTitle = "詳細表示";
                    ActionButtonText = "閉じる";
                    IsEditable = false;
                    break;
            }

            // データ初期化
            if (mode == EditorMode.Add || clip == null)
            {
                ClipData = new Clip
                {
                    SpaceId = spaceId,
                    LiveType = LiveType.Seiyuu,
                    StartTimeMs = 0,
                    Brands = BrandType.None
                };
            }
            else
            {
                // コピーを作成
                ClipData = new Clip
                {
                    Id = clip.Id,
                    SpaceId = clip.SpaceId,
                    FilePath = clip.FilePath,
                    StartTimeMs = clip.StartTimeMs,
                    EndTimeMs = clip.EndTimeMs,
                    ClipName = clip.ClipName,
                    ConcertName = clip.ConcertName,
                    Brands = clip.Brands,
                    LiveType = clip.LiveType,
                    ConcertDate = clip.ConcertDate,
                    SongTitle = clip.SongTitle,
                    Lyrics = clip.Lyrics,
                    Remarks = clip.Remarks,
                    PlayCount = clip.PlayCount,
                    ThumbnailPath = clip.ThumbnailPath,
                    IsAutoThumbnail = clip.IsAutoThumbnail,
                    CreatedAt = clip.CreatedAt,
                    UpdatedAt = DateTime.Now
                };
                if (clip.Performers != null)
                {
                    foreach (var p in clip.Performers) ClipData.Performers.Add(p);
                }
            }

            // ブランドリスト作成
            BrandList = Enum.GetValues(typeof(BrandType))
                            .Cast<BrandType>()
                            .Where(b => b != BrandType.None)
                            .Select(b => new BrandSelection
                            {
                                Name = b.ToDisplayString(),
                                Value = b,
                                IsSelected = ClipData.Brands.HasFlag(b),
                                IsEnabled = IsEditable
                            })
                            .ToList();
        }

        public void UpdateClipDuration()
        {
            long start = ClipData.StartTimeMs ?? 0;
            long end;

            // 終了時間が指定されているか？
            if (ClipData.EndTimeMs.HasValue && ClipData.EndTimeMs > 0)
            {
                end = ClipData.EndTimeMs.Value;
            }
            else
            {
                // 未指定なら動画の最後まで
                end = (long)_videoDurationMs;
            }

            // 動画自体の長さを超えないように補正
            if (_videoDurationMs > 0 && end > _videoDurationMs)
            {
                end = (long)_videoDurationMs;
            }

            long duration = end - start;
            if (duration < 0) duration = 0;

            // モデルにセット (これで DurationDisplayStr も更新される)
            ClipData.DurationMs = duration;
        }

        // 初期化処理（ViewのLoadedなどで呼ぶ）
        public async Task InitializeAsync()
        {
            if (!string.IsNullOrEmpty(ClipData.FilePath) && System.IO.File.Exists(ClipData.FilePath))
            {
                await LoadVideoDurationAsync();
            }
        }

        public async Task LoadVideoDurationAsync()
        {
            try
            {
                if (!System.IO.File.Exists(ClipData.FilePath)) return;
                var mediaInfo = await FFmpeg.GetMediaInfo(ClipData.FilePath);
                _videoDurationMs = mediaInfo.Duration.TotalMilliseconds;
                ValidateTimes();
                UpdateClipDuration();
            }
            catch
            {
                _videoDurationMs = 0;
                UpdateClipDuration();
            }
        }

        public bool ValidateTimes()
        {
            UpdateClipDuration();
            bool isValid = true;
            long startMs = 0;
            long? endMs = null;

            // 1. 書式チェック (開始)
            if (!TimeHelper.TryParseTime(ClipData.StartTimeStr, out startMs))
            {
                StartTimeErrorMessage = "※ 書式が不正です (例 10:30, 1:23:45)";
                isValid = false;
            }
            else
            {
                StartTimeErrorMessage = "";
            }

            // 2. 書式チェック (終了)
            if (string.IsNullOrWhiteSpace(ClipData.EndTimeStr))
            {
                endMs = null;
                EndTimeErrorMessage = "";
            }
            else
            {
                if (!TimeHelper.TryParseTime(ClipData.EndTimeStr, out long tempEnd))
                {
                    EndTimeErrorMessage = "※ 書式が不正です";
                    isValid = false;
                }
                else
                {
                    endMs = tempEnd;
                    EndTimeErrorMessage = "";
                }
            }

            if (!isValid) return false;

            // 3. 論理チェック (開始 < 終了)
            if (endMs.HasValue && startMs >= endMs.Value)
            {
                EndTimeErrorMessage = "※ 終了時間は開始時間より後にして下さい";
                isValid = false;
            }

            // 4. 論理チェック (動画の長さとの比較)
            if (_videoDurationMs > 0)
            {
                if (startMs > _videoDurationMs)
                {
                    StartTimeErrorMessage = "※ 動画の長さを超えています";
                    isValid = false;
                }
                if (endMs.HasValue && endMs.Value > _videoDurationMs)
                {
                    EndTimeErrorMessage = "※ 動画の長さを超えています";
                    isValid = false;
                }
            }

            return isValid;
        }

        // 保存前の最終チェックとデータ整形
        public bool CommitSave()
        {
            if (Mode == EditorMode.Detail) return true;

            if (!ValidateTimes()) return false;

            if (string.IsNullOrWhiteSpace(ClipData.FilePath))
            {
                FilePathErrorMessage = "※ 動画ファイルパスは必須です";
                return false;
            }
            FilePathErrorMessage = "";

            // ブランド反映
            var selectedBrands = BrandType.None;
            foreach (var item in BrandList)
            {
                if (item.IsSelected) selectedBrands |= item.Value;
            }
            ClipData.Brands = selectedBrands;

            return true;
        }

        public void UpdatePerformers(List<Performer> performers)
        {
            ClipData.Performers.Clear();
            foreach (var p in performers) ClipData.Performers.Add(p);
            OnPropertyChanged(nameof(PerformersDisplayText)); // 表示更新通知
        }

        // ファイルパスがセットされた時に呼ぶ
        public async Task SetFilePathAsync(string path)
        {
            ClipData.FilePath = path;
            OnPropertyChanged(nameof(ClipData)); // 画面更新
            FilePathErrorMessage = "";

            await LoadVideoDurationAsync();
            if (ClipData.IsAutoThumbnail)
            {
                await UpdateThumbnailAsync();
            }
        }

        public async Task ImportThumbnailFromFileAsync(string filePath)
        {
            try
            {
                string newPath = await _thumbnailService.ImportThumbnailAsync(filePath);

                // データの更新
                ClipData.ThumbnailPath = newPath;
                ClipData.IsAutoThumbnail = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"画像の取り込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // サムネイル手動再生成
        [RelayCommand]
        public async Task RegenerateThumbnail()
        {
            if (string.IsNullOrWhiteSpace(ClipData.FilePath)) return;
            await UpdateThumbnailAsync();
        }

        // サムネイル生成ロジック
        public async Task UpdateThumbnailAsync()
        {
            if (!System.IO.File.Exists(ClipData.FilePath)) return;

            // マウスカーソル変更はViewの責務だが、VMからは通知できないため
            // 本格的なアプリでは MouseService 等を使う。
            // ここでは簡易的に非同期処理だけ行う。

            try
            {
                long targetTimeMs = 0;
                // 動画全体の長さを取得して中央を計算
                // (LoadVideoDurationAsyncで取得済みだが、念のため再取得あるいはキャッシュ利用)
                double totalDurationMs = _videoDurationMs;
                if (totalDurationMs == 0)
                {
                    var info = await FFmpeg.GetMediaInfo(ClipData.FilePath);
                    totalDurationMs = info.Duration.TotalMilliseconds;
                }

                long start = ClipData.StartTimeMs ?? 0;
                long end = ClipData.EndTimeMs ?? (long)totalDurationMs;

                if (start > totalDurationMs) start = 0;
                if (end > totalDurationMs) end = (long)totalDurationMs;
                if (end < start) end = start;

                targetTimeMs = start + (end - start) / 2;

                string thumbPath = await _thumbnailService.GenerateThumbnailAsync(ClipData.FilePath, targetTimeMs);

                if (!string.IsNullOrEmpty(thumbPath))
                {
                    ClipData.ThumbnailPath = thumbPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "サムネイル生成エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}