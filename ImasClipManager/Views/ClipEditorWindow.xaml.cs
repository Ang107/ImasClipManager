using ImasClipManager.Helpers;
using ImasClipManager.Models;
using ImasClipManager.Services; // ThumbnailService用
using ImasClipManager.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Task用
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;   // Cursor用
using Xabe.FFmpeg;            // IMediaInfo用

namespace ImasClipManager.Views
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
        public bool IsEnabled { get; set; } = true; // 詳細モード用
    }

    public partial class ClipEditorWindow : Window
    {
        public Clip ClipData { get; private set; }
        public List<BrandSelection> BrandList { get; set; }

        // モード
        public EditorMode Mode { get; private set; }

        // 画面表示用プロパティ
        public string WindowTitle { get; private set; } = string.Empty;
        public string ActionButtonText { get; private set; } = string.Empty;
        public bool IsEditable { get; private set; }

        // エラーメッセージ用（バインディング通知は簡易的に省略し、直接代入します）
        // 本来はINotifyPropertyChangedが必要ですが、CodeBehindで直接操作します

        public ClipEditorWindow(Clip? clip = null, EditorMode mode = EditorMode.Add)
        {
            InitializeComponent();

            Mode = mode;

            // モードに応じた設定
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

            // データの初期化
            if (mode == EditorMode.Add || clip == null)
            {
                ClipData = new Clip
                {
                    SpaceId = 1,
                    ConcertDate = DateTime.Today,
                    LiveType = LiveType.Seiyuu,
                    StartTimeMs = 0,
                    Brands = BrandType.None
                };
            }
            else
            {
                // 編集・詳細時はデータをコピー
                ClipData = new Clip
                {
                    Id = clip.Id,
                    SpaceId = clip.SpaceId,
                    FilePath = clip.FilePath,
                    StartTimeMs = clip.StartTimeMs,
                    EndTimeMs = clip.EndTimeMs,
                    ConcertName = clip.ConcertName,
                    Brands = clip.Brands,
                    LiveType = clip.LiveType,
                    ConcertDate = clip.ConcertDate,
                    SongTitle = clip.SongTitle,
                    Lyrics = clip.Lyrics,
                    Remarks = clip.Remarks,
                    PlayCount = clip.PlayCount,
                    ThumbnailPath = clip.ThumbnailPath,
                    CreatedAt = clip.CreatedAt,
                    UpdatedAt = DateTime.Now
                };
                if (clip.Performers != null)
                {
                    foreach (var p in clip.Performers)
                    {
                        ClipData.Performers.Add(p);
                    }
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

            this.DataContext = this;
        }


        private void Action_Click(object sender, RoutedEventArgs e)
        {
            // 詳細モードならバリデーションなしで閉じる
            if (Mode == EditorMode.Detail)
            {
                this.DialogResult = false; // 保存しないのでfalse (あるいはCancel扱い)
                this.Close();
                return;
            }

            // バリデーション
            bool hasError = false;

            // ファイルパスエラーを直下のTextBlockに表示
            if (string.IsNullOrWhiteSpace(ClipData.FilePath))
            {
                FilePathErrorText.Text = "※ 動画ファイルパスは必須です";
                hasError = true;
                // スクロール等で見えるようにフォーカスを当てる
                FilePathBox.Focus();
            }
            else
            {
                FilePathErrorText.Text = "";
            }

            if (ClipData.EndTimeMs.HasValue && ClipData.EndTimeMs.Value <= ClipData.StartTimeMs)
            {
                MessageBox.Show("終了時刻は開始時刻より後である必要があります。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                hasError = true;
            }

            if (hasError) return;

            // ブランド反映
            var selectedBrands = BrandType.None;
            foreach (var item in BrandList)
            {
                if (item.IsSelected) selectedBrands |= item.Value;
            }
            ClipData.Brands = selectedBrands;

            this.DialogResult = true;
            this.Close();
        }

        // 画面表示用のプロパティ
        public string PerformersDisplayText
        {
            get
            {
                if (ClipData.Performers == null || !ClipData.Performers.Any()) return "(未選択)";
                // 単純結合
                return string.Join(", ", ClipData.Performers.Select(p => p.Name));
            }
        }

        private void SelectPerformers_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditable) return;

            var vm = new PerformerSelectionViewModel(ClipData.Performers.ToList());
            var window = new PerformerSelectionWindow(vm);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                var selected = vm.GetSelectedPerformers();
                ClipData.Performers.Clear();
                foreach (var p in selected) ClipData.Performers.Add(p);

                PerformersTextBox.Text = PerformersDisplayText;
            }
        }

        // 動画ファイル選択時
        private async void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditable) return;

            var dialog = new OpenFileDialog();
            dialog.Filter = "動画ファイル|*.mp4;*.mkv;*.avi;*.ts;*.m2ts;*.iso|すべてのファイル|*.*";
            if (dialog.ShowDialog() == true)
            {
                ClipData.FilePath = dialog.FileName;
                FilePathBox.Text = dialog.FileName;
                FilePathBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                FilePathErrorText.Text = "";

                // ★追加: 自動生成がONならサムネイル作成
                if (ClipData.IsAutoThumbnail)
                {
                    await UpdateThumbnailAsync();
                }
            }
        }

        // ★追加: 時間入力欄からフォーカスが外れた時
        private async void Time_LostFocus(object sender, RoutedEventArgs e)
        {
            // 値を確定させる
            if (sender is TextBox tb)
            {
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }

            // 自動生成がONなら、新しい時間に基づいて再生成
            if (ClipData.IsAutoThumbnail)
            {
                await UpdateThumbnailAsync();
            }
        }

        // ★追加: 「手動再生成」ボタン
        private async void RegenerateThumbnail_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ClipData.FilePath)) return;
            // 手動ボタンなのでフラグに関係なく実行
            await UpdateThumbnailAsync();
        }

        // ★追加: 「ファイル選択」ボタン（任意の画像を設定）
        private void SelectThumbnail_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditable) return;

            var dialog = new OpenFileDialog();
            dialog.Filter = "画像ファイル|*.jpg;*.png;*.bmp|すべてのファイル|*.*";
            if (dialog.ShowDialog() == true)
            {
                ClipData.ThumbnailPath = dialog.FileName;

                // 手動設定したので自動生成はOFFにする
                ClipData.IsAutoThumbnail = false;
            }
        }

        // ★追加: サムネイル生成のメインロジック
        private async Task UpdateThumbnailAsync()
        {
            if (!System.IO.File.Exists(ClipData.FilePath)) return;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var service = new ThumbnailService();
                // 初回のみFFmpegのダウンロードが走る
                await service.InitializeAsync();

                // 動画情報を取得して長さを確認
                var mediaInfo = await FFmpeg.GetMediaInfo(ClipData.FilePath);
                var totalDurationMs = mediaInfo.Duration.TotalMilliseconds;

                // 再生区間の中央を計算
                long start = ClipData.StartTimeMs;
                long end = ClipData.EndTimeMs ?? (long)totalDurationMs;

                // 範囲チェック
                if (start > totalDurationMs) start = 0;
                if (end > totalDurationMs) end = (long)totalDurationMs;
                if (end < start) end = start;

                // 中央時刻
                long middleTime = start + (end - start) / 2;

                // 生成実行
                string thumbPath = await service.GenerateThumbnailAsync(ClipData.FilePath, middleTime);

                if (!string.IsNullOrEmpty(thumbPath))
                {
                    ClipData.ThumbnailPath = thumbPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"サムネイル生成に失敗しました。\n\n詳細エラー:\n{ex.Message}", "エラー");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}