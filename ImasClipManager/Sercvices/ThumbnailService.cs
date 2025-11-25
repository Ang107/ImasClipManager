using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using System.Collections.Generic;

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

        public async Task<string> GenerateThumbnailAsync(string videoPath, long timeMs)
        {
            await InitializeAsync();

            try
            {
                // 1. ストリーム情報を取得 (高速)
                var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();

                if (videoStream == null) throw new Exception("動画ストリームが見つかりませんでした。");

                string fileName = $"{Guid.NewGuid()}.jpg";
                string outputPath = Path.Combine(_thumbnailFolder, fileName);

                // 時間をフォーマット
                var seekTime = TimeSpan.FromMilliseconds(timeMs).ToString(@"hh\:mm\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture);

                // 2. 高速シークコマンドの構築
                Debug.WriteLine("[ThumbnailService] 高速シークでサムネイル生成を開始します...");

                var conversion = FFmpeg.Conversions.New();

                // -ss を入力より前に置く (高速シーク)
                conversion.AddParameter($"-ss {seekTime}", ParameterPosition.PreInput);

                // AddStreamを使うことでパスの引用符処理をライブラリに任せる
                conversion.AddStream(videoStream);

                // 出力設定
                conversion.SetOutput(outputPath)
                          .SetOverwriteOutput(true)
                          .AddParameter("-frames:v 1")
                          .AddParameter("-f image2")
                          .AddParameter("-vf \"scale=-1:'min(100,ih)'\"");

                // 3. 実行
                await conversion.Start();

                return outputPath;
            }
            catch (Xabe.FFmpeg.Exceptions.ConversionException ex)
            {
                // ■ 失敗した場合: フォールバックせずに明確なエラーを返す
                var errorMsg = "サムネイルの生成に失敗しました。\n\n" +
                               "【原因の可能性】\n" +
                               "この動画ファイルは「高速シーク」に対応していない可能性があります。\n" +
                               "動画のエンコード設定で以下を確認してください：\n" +
                               "・「Web用に最適化 (Web Optimized)」がONになっているか\n" +
                               "・キーフレーム間隔 (Keyint) が適切に設定されているか\n\n" +
                               "※HandBrake等で再エンコードすると解決する場合があります。";

                // 呼び出し元のMessageBoxで表示させるためにExceptionを投げる
                throw new Exception(errorMsg, ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] 予期せぬエラー: {ex.Message}");
                throw new Exception($"予期せぬエラーが発生しました: {ex.Message}", ex);
            }
        }

        public async Task<string> ImportThumbnailAsync(string inputPath)
        {
            await InitializeAsync();

            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();

                // ストリームが見つからない場合はコピー
                if (videoStream == null)
                {
                    string ext = Path.GetExtension(inputPath);
                    string fileName = $"{Guid.NewGuid()}{ext}";
                    string outputPath = Path.Combine(_thumbnailFolder, fileName);
                    File.Copy(inputPath, outputPath, true);
                    return outputPath;
                }
                else
                {
                    string ext = Path.GetExtension(inputPath);
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    string fileName = $"{Guid.NewGuid()}{ext}";
                    string outputPath = Path.Combine(_thumbnailFolder, fileName);

                    var conversion = FFmpeg.Conversions.New();
                    conversion.AddStream(videoStream)
                              .SetOutput(outputPath)
                              .SetOverwriteOutput(true)
                              .AddParameter("-vf \"scale=-1:'min(100,ih)'\"");

                    await conversion.Start();
                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] Import Error: {ex.Message}");
                // 失敗時はコピー
                string ext = Path.GetExtension(inputPath);
                string fileName = $"{Guid.NewGuid()}{ext}";
                string outputPath = Path.Combine(_thumbnailFolder, fileName);
                File.Copy(inputPath, outputPath, true);
                return outputPath;
            }
        }

        // ★追加: 使われていないサムネイルを削除する
        // usedPaths: DBに保存されている全クリップのサムネイルパスのリスト
        public async Task<int> CleanUpUnusedThumbnailsAsync(IEnumerable<string> usedPaths)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(_thumbnailFolder)) return 0;

                // 1. 比較用にパスを正規化してHashSetにする (大文字小文字無視)
                var usedSet = new HashSet<string>(
                    usedPaths.Select(p => Path.GetFullPath(p)),
                    StringComparer.OrdinalIgnoreCase
                );

                int deletedCount = 0;

                // 2. フォルダ内の全jpg/pngファイルを取得
                var files = Directory.GetFiles(_thumbnailFolder, "*.*")
                                     .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                              || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    var fullPath = Path.GetFullPath(file);

                    // 3. DBに使われていないファイルなら削除
                    if (!usedSet.Contains(fullPath))
                    {
                        try
                        {
                            File.Delete(fullPath);
                            deletedCount++;
                            Debug.WriteLine($"[CleanUp] Deleted: {fullPath}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[CleanUp] Failed to delete {fullPath}: {ex.Message}");
                        }
                    }
                }

                return deletedCount;
            });
        }

        public void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete file: {ex.Message}");
            }
        }
    }
}