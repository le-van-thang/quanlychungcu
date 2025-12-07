using System.Windows;

namespace WpfApp1
{
    public partial class InputDialog : Window
    {
        public string Result { get; private set; }

        public InputDialog(string message, string title = "Nhập liệu")
        {
            InitializeComponent();
            Title = title;
            lblMessage.Text = message;
            Loaded += (s, e) => txtInput.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = (txtInput.Text ?? "").Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // thoát (không có kết quả)
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Result = "BACK";      // đánh dấu người dùng chọn Trở lại
            DialogResult = true;  // vẫn trả true để caller đọc được Result = "BACK"
        }
    }
}
