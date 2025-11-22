using System.Windows;

namespace ImasClipManager.Views
{
    public partial class InputWindow : Window
    {
        public string Message { get; set; } = "値を入力してください:";
        public string InputText { get; set; } = "";

        public InputWindow(string message, string defaultText = "")
        {
            InitializeComponent();
            Message = message;
            InputText = defaultText;
            this.DataContext = this;

            // ロード時にテキストボックスにフォーカス
            Loaded += (s, e) => InputTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}