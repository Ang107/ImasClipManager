using System;
using System.Windows;
using ImasClipManager.Data;
using ImasClipManager.Services;
using ImasClipManager.ViewModels;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace ImasClipManager
{
    public partial class App : Application
    {
        // アプリ全体で共有するDIプロバイダ
        public IServiceProvider Services { get; }

        // 現在のAppインスタンスを取得するためのプロパティ
        public new static App Current => (App)Application.Current;

        public App()
        {
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // --- Services (シングルトン) ---
            // ※ThumbnailServiceは初期化フラグなどを持つためSingletonが適切
            services.AddSingleton<ThumbnailService>();
            services.AddSingleton<CsvDataService>();

            // --- Database ---
            // DbContextは使用のたびに new する設計のため Transient で登録
            services.AddTransient<AppDbContext>();

            // ViewModel内で "using (var db = factory())" のように使うためのファクトリを登録
            services.AddSingleton<Func<AppDbContext>>(provider => () => provider.GetRequiredService<AppDbContext>());

            // --- ViewModels ---
            services.AddTransient<MainViewModel>();
            // ClipEditorViewModelなどは動的にパラメータ付きで生成するため、ここでは登録せずMainViewModel等から生成します

            // --- Windows ---
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // VLCエンジンの初期化
            Core.Initialize();

            var thumbnailService = Services.GetRequiredService<ThumbnailService>();
            try
            {
                await thumbnailService.InitializeAsync();
            }
            catch (Exception ex)
            {
                // 万が一初期化に失敗しても、アプリ自体は起動させる（必要に応じてログ出力）
                System.Diagnostics.Debug.WriteLine($"FFmpeg init failed: {ex.Message}");
            }

            // DIコンテナからMainWindowを取得して表示（依存関係が自動解決される）
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}