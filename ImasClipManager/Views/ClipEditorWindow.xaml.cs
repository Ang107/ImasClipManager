using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ImasClipManager.Models;
using ImasClipManager.Helpers;

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
        public string WindowTitle { get; private set; }
        public string ActionButtonText { get; private set; }
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

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditable) return;

            var dialog = new OpenFileDialog();
            dialog.Filter = "動画ファイル|*.mp4;*.mkv;*.avi;*.ts;*.m2ts;*.iso|すべてのファイル|*.*";
            if (dialog.ShowDialog() == true)
            {
                ClipData.FilePath = dialog.FileName;
                FilePathBox.Text = dialog.FileName;
                FilePathBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                // エラーが出ていれば消す
                FilePathErrorText.Text = "";
            }
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
    }
}