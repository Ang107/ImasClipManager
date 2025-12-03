using System.Windows;

namespace ImasClipManager.Views
{
    public partial class AdvancedSearchWindow : Window
    {
        public AdvancedSearchWindow()
        {
            InitializeComponent();
        }

        // ★追加: 閉じるボタンの処理
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}