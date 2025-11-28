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
        public DbSet<Playlist> Playlists { get; set; }
        // DbSetに追加
        public DbSet<DisplayState> DisplayStates { get; set; }

        // データベースの接続設定
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!Directory.Exists(Constants.AppDataFolder))
            {
                Directory.CreateDirectory(Constants.AppDataFolder);
            }
            optionsBuilder.UseSqlite($"Data Source={Constants.DbFilePath}");
        }
    }
}