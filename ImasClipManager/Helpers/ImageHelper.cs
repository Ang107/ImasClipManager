using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImasClipManager.Helpers
{
    public static class ImageHelper
    {
        public static BitmapImage LoadBitmapNoLock(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // ★重要: メモリに展開してファイルを解放
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze(); // スレッド間での共有を許可
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}