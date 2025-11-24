using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using ImasClipManager.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ImasClipManager.ViewModels
{
    public partial class VideoPlayerViewModel : ObservableObject, IDisposable
    {
        public LibVLC LibVLC { get; }
        public MediaPlayer MediaPlayer { get; }
        public Clip ClipData { get; }

        public string Title => $"{ClipData.SongTitle} - {ClipData.ConcertName}";

        // --- 画面表示用プロパティ ---

        // 現在の再生時間 (クリップ内の経過時間: 00:00)
        [ObservableProperty] private string _currentTimeStr = "00:00";
        // クリップ全体の長さ (表示用: 00:00)
        [ObservableProperty] private string _durationStr = "00:00";

        // シークバー用現在位置 (クリップ開始からの相対ミリ秒)
        [ObservableProperty] private long _currentPositionMs;
        // シークバー用最大値 (クリップの長さミリ秒)
        [ObservableProperty] private long _totalDurationMs;

        // 音量 (0-100)
        [ObservableProperty] private int _volume = 100;
        partial void OnVolumeChanged(int value) => MediaPlayer.Volume = value;

        // 再生速度 (0.25 - 2.0)
        [ObservableProperty] private float _playbackRate = 1.0f;
        partial void OnPlaybackRateChanged(float value) => MediaPlayer.SetRate(value);

        // 各種フラグ
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isRepeat;
        [ObservableProperty] private bool _isPropertyPanelVisible = true;
        [ObservableProperty] private bool _isMiniWindowMode = false;
        [ObservableProperty] private bool _isFullScreen = false;

        // 内部管理用
        private long _clipStartMs;
        private long _clipEndMs;
        private long _fileDurationMs; // 動画ファイル自体の長さ

        private readonly Dispatcher _dispatcher;
        private bool _isDraggingSeek;

        public VideoPlayerViewModel(Clip clip)
        {
            ClipData = clip;
            _dispatcher = Application.Current.Dispatcher;

            // 区間設定
            _clipStartMs = clip.StartTimeMs;
            // EndTimeMsがnullまたは0の場合は、一旦最大値にしておき、動画ロード後に補正する
            _clipEndMs = (clip.EndTimeMs.HasValue && clip.EndTimeMs > 0) ? clip.EndTimeMs.Value : long.MaxValue;

            // 初期表示用の長さを計算 (仮)
            UpdateDurationInfo();

            LibVLC = new LibVLC();
            MediaPlayer = new MediaPlayer(LibVLC);

            // イベントハンドラ登録
            MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            MediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            MediaPlayer.Playing += (s, e) => _dispatcher.Invoke(() => IsPlaying = true);
            MediaPlayer.Paused += (s, e) => _dispatcher.Invoke(() => IsPlaying = false);
            MediaPlayer.Stopped += (s, e) => _dispatcher.Invoke(() => IsPlaying = false);
            MediaPlayer.EndReached += MediaPlayer_EndReached;

            // メディアロード
            var media = new Media(LibVLC, new Uri(clip.FilePath));
            media.AddOption(":avcodec-hw=any");

            MediaPlayer.Media = media;
            MediaPlayer.Play();
            MediaPlayer.Volume = Volume;

            // 開始位置へシーク
            if (_clipStartMs > 0)
            {
                // 少し待ってからシーク（即時だと効かない場合があるため）
                Task.Delay(100).ContinueWith(_ => MediaPlayer.Time = _clipStartMs);
            }
        }

        // 時間情報の更新（長さなどが変わったときに呼ぶ）
        private void UpdateDurationInfo()
        {
            // クリップの長さを計算 (終了 - 開始)
            // まだ動画の長さが不明で _clipEndMs が MaxValue の場合は仮置き
            long duration = _clipEndMs - _clipStartMs;
            if (duration < 0) duration = 0;

            // 表示上の長さが極端に長い(未確定)場合は 00:00 にしておく等の制御も可能だが
            // ここではそのまま計算する。ただしMaxValueそのままだとUIがおかしくなるので補正。
            if (_clipEndMs == long.MaxValue) duration = 0;

            TotalDurationMs = duration;
            DurationStr = TimeSpan.FromMilliseconds(TotalDurationMs).ToString(@"mm\:ss");
        }

        private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                _fileDurationMs = e.Length;

                // クリップ終了時間が未指定(または動画長より長い)なら動画末尾をセット
                if (_clipEndMs > _fileDurationMs)
                {
                    _clipEndMs = _fileDurationMs;
                }

                // 長さ情報を再計算してUI更新
                UpdateDurationInfo();
            });
        }

        private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            // シーク操作中は更新しない
            if (_isDraggingSeek) return;

            long currentAbsTime = e.Time;

            // 区間ループ制御
            if (currentAbsTime >= _clipEndMs)
            {
                if (IsRepeat)
                {
                    // ループ: 開始位置に戻す
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => MediaPlayer.Time = _clipStartMs);
                    return;
                }
                else
                {
                    // 終了位置で一時停止
                    MediaPlayer.SetPause(true);
                    // UI上は終了位置に合わせる
                    currentAbsTime = _clipEndMs;
                }
            }

            _dispatcher.Invoke(() =>
            {
                if (_isDraggingSeek) return;
                // 相対時間を計算 (現在 - 開始)
                long relativeTime = currentAbsTime - _clipStartMs;
                if (relativeTime < 0) relativeTime = 0;

                CurrentPositionMs = relativeTime;
                CurrentTimeStr = TimeSpan.FromMilliseconds(relativeTime).ToString(@"mm\:ss");
            });
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            // ファイル末尾に到達した場合
            if (IsRepeat)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    MediaPlayer.Stop();
                    MediaPlayer.Play();
                    MediaPlayer.Time = _clipStartMs;
                });
            }
        }

        // --- コマンド ---

        [RelayCommand]
        public void TogglePlayPause()
        {
            if (MediaPlayer.IsPlaying)
                MediaPlayer.Pause();
            else
                MediaPlayer.Play();
        }

        [RelayCommand]
        public void SeekForward()
        {
            // +10秒 (絶対時間で計算)
            var target = MediaPlayer.Time + 10000;
            if (target > _clipEndMs) target = _clipEndMs;
            MediaPlayer.Time = target;
        }

        [RelayCommand]
        public void SeekBackward()
        {
            // -10秒 (絶対時間で計算)
            var target = MediaPlayer.Time - 10000;
            if (target < _clipStartMs) target = _clipStartMs;
            MediaPlayer.Time = target;
        }

        [RelayCommand]
        public void ToggleMiniWindow() => IsMiniWindowMode = !IsMiniWindowMode;

        [RelayCommand]
        public void ToggleFullScreen() => IsFullScreen = !IsFullScreen;

        // シークバー操作開始
        public void OnSeekStarted()
        {
            _isDraggingSeek = true;
        }

        // シークバー操作終了（値確定）
        public void OnSeekCompleted(long relativeTimeMs)
        {
            _isDraggingSeek = false;

            // 相対時間(UI) -> 絶対時間(VLC) に変換
            long targetAbsTime = _clipStartMs + relativeTimeMs;

            // 範囲外ガード
            if (targetAbsTime < _clipStartMs) targetAbsTime = _clipStartMs;
            if (targetAbsTime > _clipEndMs) targetAbsTime = _clipEndMs;

            MediaPlayer.Time = targetAbsTime;
        }

        public void Dispose()
        {
            MediaPlayer.Stop();
            MediaPlayer.Dispose();
            LibVLC.Dispose();
        }
    }
}