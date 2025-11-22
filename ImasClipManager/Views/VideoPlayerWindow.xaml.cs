using System.Windows;
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

            // ViewModelを作成してセット
            _viewModel = new VideoPlayerViewModel(clip);
            this.DataContext = _viewModel;

            // ウィンドウが閉じられたら、VLCのリソースを解放する
            this.Closed += (s, e) => _viewModel.Dispose();
        }
    }
}