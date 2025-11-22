using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace ImasClipManager.Services
{
    public class ThumbnailService
    {
        private static bool _isInitialized = false;
        private readonly string _thumbnailFolder;

        public ThumbnailService()
        {
            _thumbnailFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Thumbnails");
            if (!Directory.Exists(_thumbnailFolder)) Directory.CreateDirectory(_thumbnailFolder);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            string ffmpegPath = AppDomain.CurrentDomain.BaseDirectory;
            FFmpeg.SetExecutablesPath(ffmpegPath);
            if (!File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
            {
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
            }
            _isInitialized = true;
        }

        // IMediaInfo を引数に追加 (重複読み込みを避ける)
        public async Task<string> GenerateThumbnailAsync(string videoPath, IMediaInfo mediaInfo, long timeMs)
        {
            await InitializeAsync();

            try
            {
                string fileName = $"{Guid.NewGuid()}.jpg";
                string outputPath = Path.Combine(_thumbnailFolder, fileName);

                var durationMs = mediaInfo.Duration.TotalMilliseconds;
                if (timeMs > durationMs) timeMs = 0;

                // --- 高速シーク変換設定 ---
                var conversion = FFmpeg.Conversions.New();

                // 1. 高速シーク設定 (-ss を入力の前に置く)
                double seekSeconds = TimeSpan.FromMilliseconds(timeMs).TotalSeconds;
                conversion.AddParameter($"-ss {seekSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}", ParameterPosition.PreInput);

                // 2. 入力ストリーム設定 (mediaInfoから取得)
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                if (videoStream == null) throw new Exception("動画ストリームが見つかりません。");
                conversion.AddStream(videoStream);

                // 3. 出力設定
                conversion.SetOutput(outputPath)
                          .SetOverwriteOutput(true)
                          .AddParameter("-frames:v 1");

                // 4. タイムアウト付き実行
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        await conversion.Start(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new Exception("処理がタイムアウトしました（15秒経過）。ファイルが大きすぎるか、処理が詰まっています。");
                    }
                }

                return outputPath;
            }
            catch (Xabe.FFmpeg.Exceptions.ConversionException ce)
            {
                throw new Exception($"FFmpeg変換エラー: {ce.Message}\n\n実行コマンド: {ce.InputParameters}", ce);
            }
            catch (Exception ex)
            {
                throw new Exception($"サムネイル生成エラー: {ex.Message}", ex);
            }
        }
    }
}