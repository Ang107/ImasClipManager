using ImasClipManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace ImasClipManager.Data
{
    public class AppDbContext : DbContext
    {
        // テーブルの定義
        public DbSet<Clip> Clips { get; set; }
        public DbSet<Space> Spaces { get; set; }
        public DbSet<Performer> Performers { get; set; }

        // データベースの接続設定
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ローカルアプリケーションデータフォルダ (例: C:\Users\User\AppData\Local\ImasClipManager) を取得
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImasClipManager");

            // フォルダがなければ作成
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var dbPath = Path.Combine(folder, "ImasClipManager.db");

            // フルパスを指定してSQLiteを使用
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}