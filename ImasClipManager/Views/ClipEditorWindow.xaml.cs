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
using System.Diagnostics;

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

        private double _videoDurationMs = 0;

        // エラーメッセージ用（バインディング通知は簡易的に省略し、直接代入します）
        // 本来はINotifyPropertyChangedが必要ですが、CodeBehindで直接操作します

        public ClipEditorWindow(Clip? clip = null, EditorMode mode = EditorMode.Add)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
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
                    IsAutoThumbnail = clip.IsAutoThumbnail,
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

            if (!string.IsNullOrEmpty(ClipData.FilePath) && System.IO.File.Exists(ClipData.FilePath))
            {
                this.Loaded += async (s, e) => await LoadVideoDurationAsync();
            }
        }

        private async Task LoadVideoDurationAsync()
        {
            try
            {
                if (!System.IO.File.Exists(ClipData.FilePath)) return;
                var mediaInfo = await FFmpeg.GetMediaInfo(ClipData.FilePath);
                _videoDurationMs = mediaInfo.Duration.TotalMilliseconds;

                // 長さが取れたら一度バリデーションしておく
                ValidateTimes();
            }
            catch
            {
                _videoDurationMs = 0;
            }
        }

        private bool ValidateTimes()
        {
            bool isValid = true;
            long startMs = 0;
            long? endMs = null;

            // 1. 書式チェック (開始)
            if (!TimeHelper.TryParseTime(StartTimeBox.Text, out startMs))
            {
                StartTimeErrorText.Text = "※ 書式が不正です (例 10:30, 1:23:45)";
                isValid = false;
            }
            else
            {
                StartTimeErrorText.Text = "";
            }

            // 2. 書式チェック (終了)
            if (string.IsNullOrWhiteSpace(EndTimeBox.Text))
            {
                endMs = null;
                EndTimeErrorText.Text = "";
            }
            else
            {
                if (!TimeHelper.TryParseTime(EndTimeBox.Text, out long tempEnd))
                {
                    EndTimeErrorText.Text = "※ 書式が不正です";
                    isValid = false;
                }
                else
                {
                    endMs = tempEnd;
                    EndTimeErrorText.Text = "";
                }
            }

            if (!isValid) return false;

            // 3. 論理チェック (開始 < 終了)
            if (endMs.HasValue && startMs >= endMs.Value)
            {
                EndTimeErrorText.Text = "※ 終了時間は開始時間より後にして下さい";
                isValid = false;
            }

            // 4. 論理チェック (動画の長さとの比較)
            if (_videoDurationMs > 0)
            {
                if (startMs > _videoDurationMs)
                {
                    StartTimeErrorText.Text = "※ 動画の長さを超えています";
                    isValid = false;
                }

                if (endMs.HasValue && endMs.Value > _videoDurationMs)
                {
                    EndTimeErrorText.Text = "※ 動画の長さを超えています";
                    isValid = false;
                }
            }

            return isValid;
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // フォーカスが当たっているのがTextBoxの場合
                if (Keyboard.FocusedElement is TextBox textBox)
                {
                    // 複数行入力(AcceptsReturn=True)の場合は、普通の改行として扱うので何もしない
                    if (textBox.AcceptsReturn) return;

                    // 単一行入力の場合は、次のコントロールへフォーカス移動（＝入力完了）
                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    if (textBox.MoveFocus(request))
                    {
                        e.Handled = true; // イベントを処理済みにする（ビープ音防止など）
                    }
                }
            }
        }

        // 既存メソッドの修正: 保存ボタンクリック時
        private void Action_Click(object sender, RoutedEventArgs e)
        {
            if (Mode == EditorMode.Detail)
            {
                this.DialogResult = false;
                this.Close();
                return;
            }

            // ★追加: 保存前に強制バリデーション
            // 時間のバリデーション (エラーメッセージも更新される)
            if (!ValidateTimes())
            {
                return; // エラーがあれば保存させない
            }

            // ファイルパス必須チェック
            if (string.IsNullOrWhiteSpace(ClipData.FilePath))
            {
                FilePathErrorText.Text = "※ 動画ファイルパスは必須です";
                FilePathBox.Focus();
                return;
            }
            else
            {
                FilePathErrorText.Text = "";
            }

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
                // ★変更: カンマ区切りではなく、改行区切りにする
                return string.Join(Environment.NewLine, ClipData.Performers.Select(p => p.Name));
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
                await LoadVideoDurationAsync();
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
            ValidateTimes();
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
        // サムネイル生成のメインロジック
        private async Task UpdateThumbnailAsync()
        {
            if (!System.IO.File.Exists(ClipData.FilePath)) return;

            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                var service = new ThumbnailService();
                await service.InitializeAsync();

                long targetTimeMs = ClipData.StartTimeMs;

                // 自動生成の場合は、GetMediaInfo で長さを取得して中央時間を計算
                if (ClipData.IsAutoThumbnail)
                {
                    // ユーザー報告: GetMediaInfo は高速なのでそのまま使用
                    var mediaInfo = await Task.Run(() => FFmpeg.GetMediaInfo(ClipData.FilePath));
                    var totalDurationMs = mediaInfo.Duration.TotalMilliseconds;

                    long start = ClipData.StartTimeMs;
                    long end = ClipData.EndTimeMs ?? (long)totalDurationMs;

                    if (start > totalDurationMs) start = 0;
                    if (end > totalDurationMs) end = (long)totalDurationMs;
                    if (end < start) end = start;

                    targetTimeMs = start + (end - start) / 2;
                }

                // GenerateThumbnailAsync を呼び出し
                string thumbPath = await Task.Run(async () =>
                {
                    return await service.GenerateThumbnailAsync(ClipData.FilePath, targetTimeMs);
                });

                if (!string.IsNullOrEmpty(thumbPath))
                {
                    ClipData.ThumbnailPath = thumbPath;
                }
            }
            catch (Exception ex)
            {
                // エラー内容を表示
                MessageBox.Show($"{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}