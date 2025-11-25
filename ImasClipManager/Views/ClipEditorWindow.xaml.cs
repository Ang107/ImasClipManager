using ImasClipManager.Models;
using ImasClipManager.ViewModels;
using ImasClipManager.Views;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImasClipManager.Services;

namespace ImasClipManager.Views
{
    // BrandSelection, EditorMode は ViewModel 側に移動したので削除

    public partial class ClipEditorWindow : Window
    {
        // 外部からデータを取り出すためのプロパティ（ViewModelへのショートカット）
        public Clip ClipData => _viewModel.ClipData;

        private ClipEditorViewModel _viewModel;

        public ClipEditorWindow(ClipEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
            this.KeyDown += Window_KeyDown;

            this.Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement is TextBox textBox)
                {
                    if (textBox.AcceptsReturn) return;
                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    if (textBox.MoveFocus(request))
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        // 保存ボタン
        private void Action_Click(object sender, RoutedEventArgs e)
        {
            // ViewModelの検証・保存前処理を実行
            if (_viewModel.CommitSave())
            {
                this.DialogResult = true;
                this.Close();
            }
            // 失敗時はViewModel側でエラーメッセージがセットされるので何もしない
        }

        // 出演者選択 (画面遷移はViewの責務として残すパターン)
        private void SelectPerformers_Click(object sender, RoutedEventArgs e)
        {
            var vm = new PerformerSelectionViewModel(_viewModel.ClipData.Performers.ToList());
            var window = new PerformerSelectionWindow(vm);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                var selected = vm.GetSelectedPerformers();
                _viewModel.UpdatePerformers(selected);
            }
        }

        // ファイル選択 (ダイアログ表示はViewで行い、結果をVMに渡す)
        private async void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "動画ファイル|*.mp4;*.mkv;*.avi;*.ts;*.m2ts;*.iso|すべてのファイル|*.*";
            if (dialog.ShowDialog() == true)
            {
                await _viewModel.SetFilePathAsync(dialog.FileName);
            }
        }

        // サムネイル画像選択
        private async void SelectThumbnail_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "画像ファイル|*.jpg;*.png;*.bmp|すべてのファイル|*.*";

            if (dialog.ShowDialog() == true)
            {
                // ViewModelにお願いするだけにする
                await _viewModel.ImportThumbnailFromFileAsync(dialog.FileName);
            }
        }

        private void Time_LostFocus(object sender, RoutedEventArgs e)
        {
            // バリデーションのトリガー
            if (sender is TextBox tb)
            {
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
            _viewModel.ValidateTimes();

            if (_viewModel.ClipData.IsAutoThumbnail)
            {
                _ = _viewModel.UpdateThumbnailAsync();
            }
        }
    }
}