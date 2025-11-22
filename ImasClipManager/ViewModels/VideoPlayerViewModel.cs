using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;
using ImasClipManager.Models;
using System;

namespace ImasClipManager.ViewModels
{
    public class VideoPlayerViewModel : ObservableObject, IDisposable
    {
        public LibVLC LibVLC { get; }
        public MediaPlayer MediaPlayer { get; }
        public Clip ClipData { get; }

        // ウィンドウのタイトル
        public string Title => $"{ClipData.SongTitle} - {ClipData.ConcertName}";

        public VideoPlayerViewModel(Clip clip)
        {
            ClipData = clip;

            // VLCエンジンの初期化
            LibVLC = new LibVLC();
            MediaPlayer = new MediaPlayer(LibVLC);

            // 動画メディアのロード
            // 日本語パスなどのトラブル防止のため、Uri形式で渡します
            var media = new Media(LibVLC, new Uri(clip.FilePath));

            // 再生オプション（HWアクセラレーションなど）は必要に応じてここで設定

            MediaPlayer.Media = media;
            MediaPlayer.Play();

            // 指定位置からの再生
            if (clip.StartTimeMs > 0)
            {
                // Play直後は効かない場合があるため、少し待つかイベントで処理するのが確実ですが、
                // VLCSharpはPlay直後のTimeセットも概ね受け付けます
                MediaPlayer.Time = clip.StartTimeMs;
            }
        }

        // ウィンドウが閉じられたときに呼ばれる終了処理
        public void Dispose()
        {
            MediaPlayer.Stop();
            MediaPlayer.Dispose();
            LibVLC.Dispose();
        }
    }
}