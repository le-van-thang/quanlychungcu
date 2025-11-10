using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class DashboardWindow : Window
    {
        private readonly TaiKhoan _currentUser;

        public DashboardWindow(TaiKhoan user)
        {
            InitializeComponent();

            if (user == null)
            {
                MessageBox.Show("Không có thông tin đăng nhập. Vui lòng đăng nhập lại.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            _currentUser = user;

            // Hiển thị tên user trên Title
            var userLabel = TryGet(_currentUser, "HoTen")
                            ?? TryGet(_currentUser, "FullName")
                            ?? TryGet(_currentUser, "Username")
                            ?? "(unknown)";
            Title = $"Trang chủ - Quản lý chung cư — {userLabel}";

            // Thiết lập menu theo vai trò
            SetupMenuByRole();

            // Màn hình mặc định
            Navigate(new HomeControl(_currentUser));
        }

        private void SetupMenuByRole()
        {
            var role = TryGet(_currentUser, "VaiTro")?.ToLowerInvariant() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quantri";

            btnTaiKhoanList.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnMenuVatTu.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            expTaiChinh.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            expPhuongTien.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnMenuKhuVucThuongMai.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Cư dân + Tài khoản cá nhân thì ai cũng thấy
            expKhuVucDanCu.Visibility = Visibility.Visible;
            expTaiKhoan.Visibility = Visibility.Visible;
        }

        private void Navigate(UserControl control)
        {
            try { MainContent.Content = control; }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở màn hình:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string TryGet(object obj, string propName)
        {
            var pi = obj.GetType().GetProperty(propName);
            var v = pi?.GetValue(obj);
            return v?.ToString();
        }

        // ============== Menu handlers ==============
        private void BtnHome_Click(object sender, RoutedEventArgs e)
            => Navigate(new HomeControl(_currentUser));

        private void BtnKhuVucThuongMai_Click(object sender, RoutedEventArgs e)
            => Navigate(new MatBangThuongMaiControl());

        private void BtnVatTu_Click(object sender, RoutedEventArgs e)
            => Navigate(new VatTuControl());

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var login = new LoginWindow();
                login.Show();
                Application.Current.MainWindow = login;
                this.Close();
            }
        }

        // Dân cư
        private void BtnCuDan_Click(object sender, RoutedEventArgs e)
            => Navigate(new CuDanControl(_currentUser));

        private void BtnCanHo_Click(object sender, RoutedEventArgs e)
            => Navigate(new CanHoControl(_currentUser));

        // Tài chính
        private void BtnHoaDonCuDan_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HoaDonCuDanControl(_currentUser);
        }

        // 🏢 Hóa đơn thương mại
        private void BtnHoaDonThuongMai_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HoaDonThuongMaiControl();
        }

        // Phương tiện
        private void BtnXeOTo_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeOToControl());

        private void BtnXeMay_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeMayControl());

        private void BtnXeDap_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeDapControl());

        // Tài khoản
        private void BtnTaiKhoanInfo_Click(object sender, RoutedEventArgs e)
            => Navigate(new TaiKhoanInfoControl(_currentUser));

        private void BtnTaiKhoanList_Click(object sender, RoutedEventArgs e)
            => Navigate(new TaiKhoanListControl());
    }
}
