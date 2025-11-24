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
            this.Loaded += (s, e) => _viewModel.Loaded();
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
        // シークバー操作: マウスダウンで更新停止、位置更新、マウスキャプチャ開始
        private void Slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                // 左クリックのみ処理する
                if (e.ChangedButton == MouseButton.Left)
                {
                    // 1. ViewModelへ操作開始を通知（再生位置の更新をロックさせる）
                    _viewModel.OnSeekStarted();

                    // 2. マウス位置からシークバーの値を計算して即座に反映
                    UpdateSliderValueFromMouse(slider, e);

                    // 3. マウスをキャプチャして、スライダー外に出てもドラッグを継続できるようにする
                    slider.CaptureMouse();

                    // 標準の動作（つまみ移動のみでドラッグに移行しない挙動）をキャンセル
                    e.Handled = true;
                }
            }
        }

        // ドラッグ中の処理
        private void Slider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider)
            {
                // マウスキャプチャ中（ドラッグ中）なら値を更新し続ける
                if (slider.IsMouseCaptured)
                {
                    UpdateSliderValueFromMouse(slider, e);
                    // ここではHandledにせず、イベントを流しても良いが、念のため
                    // e.Handled = true; 
                }
            }
        }

        // 操作終了
        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                if (slider.IsMouseCaptured)
                {
                    // キャプチャを解放
                    slider.ReleaseMouseCapture();

                    // 最終的な位置でシークを実行
                    _viewModel.OnSeekCompleted((long)slider.Value);

                    e.Handled = true;
                }
            }
        }

        // マウス位置からSliderの値を計算するヘルパーメソッド
        private void UpdateSliderValueFromMouse(Slider slider, MouseEventArgs e)
        {
            // Slider上のマウス位置を取得
            Point point = e.GetPosition(slider);

            // 割合を計算 (0.0 ～ 1.0)
            double ratio = point.X / slider.ActualWidth;

            // 範囲外を丸める
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            // 値に変換してセット
            double newValue = ratio * (slider.Maximum - slider.Minimum) + slider.Minimum;
            slider.Value = newValue;
        }

        private void VolumeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    // 1. 即座に値を反映
                    UpdateSliderValueFromMouse(slider, e);

                    // 2. マウスをキャプチャしてドラッグ操作を開始
                    slider.CaptureMouse();

                    e.Handled = true;
                }
            }
        }

        private void VolumeSlider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider)
            {
                // ドラッグ中なら値を更新し続ける
                if (slider.IsMouseCaptured)
                {
                    UpdateSliderValueFromMouse(slider, e);
                }
            }
        }

        private void VolumeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                if (slider.IsMouseCaptured)
                {
                    // キャプチャを解放して終了
                    slider.ReleaseMouseCapture();
                    e.Handled = true;
                }
            }
        }

        // コントロールの表示制御などを入れたい場合は MouseMove 等を使用
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            // 必要に応じてコントロールのFadeIn/Outなどを実装
        }
    }
}