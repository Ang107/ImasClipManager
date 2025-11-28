using System;
using System.IO;

namespace ImasClipManager
{
    public static class Constants
    {
        // アプリケーションデータフォルダ (例: C:\Users\xxx\AppData\Local\ImasClipManager)
        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImasClipManager");

        // DBパス
        public static readonly string DbFilePath = Path.Combine(AppDataFolder, "ImasClipManager.db");

        // サムネイルフォルダ
        public static readonly string ThumbnailFolder = Path.Combine(AppDataFolder, "Thumbnails");
    }
}