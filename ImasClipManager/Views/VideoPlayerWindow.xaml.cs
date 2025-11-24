using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImasClipManager.Models;
using ImasClipManager.ViewModels;

namespace ImasClipManager.Views
{
    public partial class VideoPlayerWindow : Window
    {
        private VideoPlayerViewModel _viewModel;

        public VideoPlayerWindow(Clip clip)
        {
            InitializeComponent();

            _viewModel = new VideoPlayerViewModel(clip);
            this.DataContext = _viewModel;

            // ViewModelのプロパティ変更通知を購読してウィンドウの状態を変える
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            this.Closed += (s, e) =>
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.Dispose();
            };
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VideoPlayerViewModel.IsFullScreen))
            {
                if (_viewModel.IsFullScreen)
                {
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                }
            }
        }

        // シークバー操作: マウスダウンで更新停止、マウスアップでシーク実行
        private void Slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.OnSeekStarted();
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                _viewModel.OnSeekCompleted((long)slider.Value);
            }
        }

        // コントロールの表示制御などを入れたい場合は MouseMove 等を使用
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            // 必要に応じてコントロールのFadeIn/Outなどを実装
        }
    }
}