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

        // スペースが追加されたら自動的にスクロールする
        private void Spaces_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
            {
                var newItem = e.NewItems[0];
                // UI描画を待ってからスクロール
                Dispatcher.InvokeAsync(() =>
                {
                    SpaceListBox.ScrollIntoView(newItem);
                    SpaceListBox.SelectedItem = newItem;
                }, DispatcherPriority.Background);
            }
        }

        private void SpaceNameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 表示されたタイミング（Visibleになったとき）のみ処理する
            if (sender is TextBox textBox && textBox.Visibility == Visibility.Visible)
            {
                // UIの描画タイミングとずれないよう、少し待ってからフォーカスする
                Dispatcher.InvokeAsync(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }, DispatcherPriority.Input);
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