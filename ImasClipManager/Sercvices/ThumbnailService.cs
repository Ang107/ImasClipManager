using System;
using System.IO;
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
            // 実行ファイル(exe)のある場所に "Thumbnails" フォルダを作る
            _thumbnailFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Thumbnails");
            if (!Directory.Exists(_thumbnailFolder))
            {
                Directory.CreateDirectory(_thumbnailFolder);
            }
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            // FFmpegの実行ファイルがなければダウンロードする (初回のみ時間がかかります)
            // 保存先はアプリ実行フォルダ
            string ffmpegPath = AppDomain.CurrentDomain.BaseDirectory;
            FFmpeg.SetExecutablesPath(ffmpegPath);

            if (!File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
            {
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
            }

            _isInitialized = true;
        }

        public async Task<string> GenerateThumbnailAsync(string videoPath, long timeMs)
        {
            await InitializeAsync();

            try
            {
                // 出力ファイル名 (GUIDでユニーク化)
                string fileName = $"{Guid.NewGuid()}.jpg";
                string outputPath = Path.Combine(_thumbnailFolder, fileName);

                // 動画情報を取得
                var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);

                // 指定時間が動画の長さを超えていないかチェック
                var durationMs = mediaInfo.Duration.TotalMilliseconds;
                if (timeMs > durationMs) timeMs = 0;

                // スナップショットを取得
                // TakeSnapshot(input, output, width, height, time)
                // width/heightを0にすると元サイズを維持しますが、
                // ここではサムネ用に少し縮小しても良いかもしれません (例: 480, 270)
                // 今回は元サイズ(0,0)で取得します。
                var conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(
                    videoPath,
                    outputPath,
                    TimeSpan.FromMilliseconds(timeMs)
                );
                await conversion.Start();
                return outputPath;
            }
            catch (Xabe.FFmpeg.Exceptions.ConversionException ce)
            {
                // ★追加: FFmpeg特有のエラー（引数間違いなど）の詳細を取得
                throw new Exception($"FFmpeg変換エラー: {ce.Message}\nInput: {ce.InputParameters}", ce);
            }
            catch (Exception ex)
            {
                throw new Exception($"エラー: {ex.Message}", ex);
            }
        }
    }
}