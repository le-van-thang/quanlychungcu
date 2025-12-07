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

            // ===== Tiêu đề cửa sổ =====
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

            // ===== Phân quyền menu =====
            SetupMenuByRole();

            // ===== Mặc định vào trang chủ =====
            Navigate(new HomeControl(_currentUser));
        }

        private void SetupMenuByRole()
        {
            bool isAdmin = RoleHelper.IsAdmin(_currentUser);
            bool isManager = RoleHelper.IsManager(_currentUser);
            bool isUser = RoleHelper.IsUser(_currentUser);

            // CẢ 3 ROLE đều thấy tất cả module nghiệp vụ
            expKhuVucDanCu.Visibility = Visibility.Visible;
            btnMenuKhuVucThuongMai.Visibility = Visibility.Visible;
            btnMenuVatTu.Visibility = Visibility.Visible;
            expTaiChinh.Visibility = Visibility.Visible;
            expPhuongTien.Visibility = Visibility.Visible;
            expTaiKhoan.Visibility = Visibility.Visible;

            if (isAdmin)
            {
                // Admin thấy luôn "Tất cả tài khoản"
                btnTaiKhoanList.Visibility = Visibility.Visible;
            }
            else
            {
                // User + Manager KHÔNG thấy danh sách tài khoản
                btnTaiKhoanList.Visibility = Visibility.Collapsed;
            }
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

        // ========== Menu click handlers ==========

        private void BtnHome_Click(object sender, RoutedEventArgs e)
            => Navigate(new HomeControl(_currentUser));

        private void BtnKhuVucThuongMai_Click(object sender, RoutedEventArgs e)
            => Navigate(new MatBangThuongMaiControl(_currentUser));

        private void BtnVatTu_Click(object sender, RoutedEventArgs e)
            => Navigate(new VatTuControl(_currentUser));

        private void BtnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AuditLogger.Log("Logout", "Auth",
                    _currentUser?.TaiKhoanID.ToString(),
                    "Đăng xuất khỏi ứng dụng");

                // Xoá session hiện tại
                SessionStore.Clear();

                //  xoá luôn thông tin "Ghi nhớ tài khoản và mật khẩu"
                RememberStore.Clear();

                // Quay về màn đăng nhập
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
            => Navigate(new HoaDonCuDanControl(_currentUser));

        private void BtnHoaDonThuongMai_Click(object sender, RoutedEventArgs e)
            => Navigate(new HoaDonThuongMaiControl(_currentUser));

        private void BtnXeOTo_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeOToControl(_currentUser));

        private void BtnXeMay_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeMayControl(_currentUser));

        private void BtnXeDap_Click(object sender, RoutedEventArgs e)
            => Navigate(new XeDapControl(_currentUser));

        private void BtnTaiKhoanInfo_Click(object sender, RoutedEventArgs e)
            => Navigate(new TaiKhoanInfoControl(_currentUser));

        private void BtnTaiKhoanList_Click(object sender, RoutedEventArgs e)
            => Navigate(new TaiKhoanListControl(_currentUser));
    }
}
