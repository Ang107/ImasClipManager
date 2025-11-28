using ImasClipManager.Models;
using ImasClipManager.ViewModels; // 追加
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Specialized; // 追加
using System.Windows.Threading; // 追加

namespace ImasClipManager
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            
            // ここでViewModelを画面にセットします
            this.DataContext = viewModel;
            viewModel.Spaces.CollectionChanged += Spaces_CollectionChanged;
        }

        // ImasClipManager/MainWindow.xaml.cs

        // 1. スクロール処理：元の「Background」に戻す
        private void Spaces_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
            {
                var newItem = e.NewItems[0];
                // 優先度 Background (4) で実行
                Dispatcher.InvokeAsync(() =>
                {
                    SpaceListBox.ScrollIntoView(newItem);
                }, DispatcherPriority.Background);
            }
        }

        // 2. 編集時用（ダブルクリックなど）：Loadedが走らないのでこちらで処理
        private void SpaceNameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Visibility == Visibility.Visible)
            {
                // 既存スペースの編集時のみ実行（新規はLoadedに任せる）
                if (textBox.DataContext is Space space && !space.IsNew)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        textBox.Focus();
                        textBox.SelectAll();
                    }, DispatcherPriority.Input);
                }
            }
        }

        // 3. 新規追加用：元のコードと同じ確実なタイミングで処理
        private void SpaceNameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 新規追加時のみ実行
                if (textBox.DataContext is Space space && space.IsNew)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            }
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