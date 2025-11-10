using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class HomeControl : UserControl
    {
        private TaiKhoan currentUser;

        public HomeControl(TaiKhoan user)
        {
            InitializeComponent();
            currentUser = user;

            if (currentUser != null)
                txtWelcome.Text = $"Xin chào {currentUser.Username} ({currentUser.VaiTro})";
            else
                txtWelcome.Text = "Xin chào";

            LoadDashboardData();
        }

        public HomeControl()
        {
            InitializeComponent();
            txtWelcome.Text = "Xin chào";
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                using (var db = new QuanlychungcuEntities())
                {
                    txtSoCanHo.Text = db.CanHoes.Count().ToString();
                    txtSoCuDan.Text = db.CuDans.Count().ToString();
                    txtSoOto.Text = db.XeOToes.Count().ToString();
                    txtSoXeMay.Text = db.XeMays.Count().ToString();
                    txtSoXeDap.Text = db.XeDaps.Count().ToString();
                }
            }
            catch
            {
                MessageBox.Show("Không thể tải dữ liệu thống kê!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Navigate(UserControl uc)
        {
            var shell = Window.GetWindow(this) as DashboardWindow;
            if (shell?.MainContent != null)
            {
                shell.MainContent.Content = uc;
            }
            else
            {
                MessageBox.Show("Không tìm thấy vùng nội dung để điều hướng.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCanHo_Click(object sender, RoutedEventArgs e)
            => Navigate(new CanHoControl(currentUser));

        private void BtnCuDan_Click(object sender, RoutedEventArgs e)
            => Navigate(new CuDanControl(currentUser));

        private void BtnOto_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeOToControl(currentUser));

        private void BtnXeMay_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeMayControl(currentUser));

        private void BtnXeDap_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeDapControl(currentUser));

        // NEW: mở cửa sổ AI
        private void BtnAI_Click(object sender, RoutedEventArgs e)
        {
            var win = new AIWindow(currentUser);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }
    }
}
