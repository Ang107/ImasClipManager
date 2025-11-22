using ImasClipManager.Models;
using ImasClipManager.ViewModels; // 追加
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ダブルクリックされた場所が「行」であり、データ(Clip)が取れるか確認
            if (sender is DataGrid grid &&
                grid.SelectedItem is Clip selectedClip &&
                this.DataContext is MainViewModel vm)
            {
                // ViewModelの再生メソッドを直接呼び出す
                vm.PlayClip(selectedClip);
            }
        }
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}