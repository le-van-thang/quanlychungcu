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

            string displayName = TryGet(_currentUser, "HoTen")
                                 ?? TryGet(_currentUser, "FullName")
                                 ?? TryGet(_currentUser, "Email");

            if (string.IsNullOrWhiteSpace(displayName))
            {
                var username = TryGet(_currentUser, "Username");
                displayName = (username != null && username.StartsWith("Facebook:", StringComparison.OrdinalIgnoreCase))
                                ? username.Substring("Facebook:".Length)
                                : (username ?? "(unknown)");
            }

            Title = $"Trang chủ - Quản lý chung cư — {displayName}";

            SetupMenuByRole();

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
                SessionStore.Clear();

                var login = new LoginWindow();
                login.Show();
                Application.Current.MainWindow = login;
                this.Close();
            }
        }

        private void BtnCuDan_Click(object sender, RoutedEventArgs e)
            => Navigate(new CuDanControl(_currentUser));

        private void BtnCanHo_Click(object sender, RoutedEventArgs e)
            => Navigate(new CanHoControl(_currentUser));

        private void BtnHoaDonCuDan_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HoaDonCuDanControl(_currentUser);
        }

        private void BtnHoaDonThuongMai_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HoaDonThuongMaiControl();
        }

        private void BtnXeOTo_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeOToControl());

        private void BtnXeMay_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeMayControl());

        private void BtnXeDap_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeDapControl());

        private void BtnTaiKhoanInfo_Click(object sender, RoutedEventArgs e)
            => Navigate(new TaiKhoanInfoControl(_currentUser));

        private void BtnTaiKhoanList_Click(object sender, RoutedEventArgs e)
            => Navigate(new TaiKhoanListControl());
    }
}
