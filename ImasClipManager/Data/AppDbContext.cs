using ImasClipManager.Models;
using Microsoft.EntityFrameworkCore;

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
            // アプリの実行フォルダに "ImasClipManager.db" というファイルを作ります
            optionsBuilder.UseSqlite("Data Source=ImasClipManager.db");
        }
    }
}