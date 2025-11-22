using System.Windows;
using ImasClipManager.ViewModels;

namespace ImasClipManager.Views
{
    public partial class PerformerSelectionWindow : Window
    {
        public PerformerSelectionWindow(PerformerSelectionViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}