using ImasClipManager.Models;
using ImasClipManager.ViewModels; // 追加
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImasClipManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // ここでViewModelを画面にセットします
            this.DataContext = new MainViewModel();
        }
        // ▼ 修正: クリック位置が行(DataGridRow)内かチェックする
        private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // クリックされた大元の要素(テキストブロックやボーダーなど)を取得
            var source = e.OriginalSource as DependencyObject;

            // VisualTreeを親に向かって遡り、DataGridRowを探す
            while (source != null && source != sender)
            {
                if (source is DataGridRow)
                {
                    // 行が見つかった場合のみ再生処理へ進む
                    if (sender is DataGrid grid &&
                        grid.SelectedItem is Clip selectedClip &&
                        this.DataContext is MainViewModel vm)
                    {
                        await vm.PlayClip(selectedClip);
                    }
                    return; // 処理完了
                }
                // 親要素へ移動
                source = VisualTreeHelper.GetParent(source);
            }

            // ここに来た場合は、行以外の場所（余白やヘッダーなど）がクリックされたということなので何もしない
        }
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}