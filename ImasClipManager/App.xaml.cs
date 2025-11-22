using System.Windows;
using LibVLCSharp.Shared; // 追加

namespace ImasClipManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // VLCエンジンの初期化
            Core.Initialize();
        }
    }
}