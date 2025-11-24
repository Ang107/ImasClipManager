using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ImasClipManager.Views
{
    public partial class InputWindow : Window, INotifyPropertyChanged
    {
        // --- INotifyPropertyChanged 実装 ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // --- プロパティ ---

        public string Message { get; set; } = "値を入力してください:";

        private string _inputText = "";
        public string InputText
        {
            get => _inputText;
            set
            {
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged();
                    // 文字が入力されたらエラーをクリアする等の親切設計
                    if (!string.IsNullOrEmpty(ErrorMessage))
                    {
                        ErrorMessage = string.Empty;
                    }
                }
            }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // バリデーション用デリゲート (入力値を受け取り、エラーならメッセージを返す。正常ならnull)
        public Func<string, string?>? Validator { get; set; }

        // --- コンストラクタ ---

        public InputWindow(string message, string defaultText = "")
        {
            InitializeComponent();
            Message = message;
            _inputText = defaultText; // 初期値セット(プロパティ経由だと通知が飛ぶのでフィールドへ)

            this.DataContext = this;

            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll(); // 開いたときに全選択状態にする
            };
        }

        // --- イベントハンドラ ---

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // バリデーション実行
            if (Validator != null)
            {
                var error = Validator(InputText);
                if (!string.IsNullOrEmpty(error))
                {
                    ErrorMessage = error;
                    return; // エラーがある場合は閉じない
                }
            }

            DialogResult = true;
            Close();
        }
    }
}