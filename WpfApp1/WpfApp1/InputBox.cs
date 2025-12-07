using System.Windows;

namespace WpfApp1
{
    public static class InputBox
    {
        public static string Show(string message, string title = "Nhập liệu")
        {
            var dlg = new InputDialog(message, title);
            bool? ok = dlg.ShowDialog();
            return ok == true ? dlg.Result : null;
        }
    }
}
