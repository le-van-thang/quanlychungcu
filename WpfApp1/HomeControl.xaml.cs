using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;   // <-- THÊM DÒNG NÀY

namespace WpfApp1
{
    public partial class HomeControl : UserControl
    {
        private TaiKhoan currentUser;

        public HomeControl(TaiKhoan user)
        {
            InitializeComponent();
            currentUser = user;

            var display = ComputeDisplayName(currentUser);
            txtWelcome.Text = currentUser != null
                ? $"Xin chào {display} ({currentUser.VaiTro})"
                : "Xin chào";

            ApplyRoleUi();

            // Chỉ load DB khi đang RUN, không phải khi mở trong Designer
            if (!DesignerProperties.GetIsInDesignMode(this))
                LoadDashboardData();
        }

        public HomeControl()
        {
            InitializeComponent();
            txtWelcome.Text = "Xin chào";

            ApplyRoleUi();

            if (!DesignerProperties.GetIsInDesignMode(this))
                LoadDashboardData();
        }

        /// <summary>
        /// Ẩn/hiện các nút trên trang chủ theo vai trò
        /// </summary>
        private void ApplyRoleUi()
        {
            bool isAdmin = RoleHelper.IsAdmin(currentUser);
            bool isManager = RoleHelper.IsManager(currentUser);
            bool canViewLog = isAdmin || isManager;

            // User thường: không thấy nút nhật ký
            if (!canViewLog && btnViewLog != null)
                btnViewLog.Visibility = Visibility.Collapsed;
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

        private static string ComputeDisplayName(TaiKhoan tk)
        {
            if (tk == null) return "";

            try
            {
                using (var db = new QuanlychungcuEntities())
                {
                    var oa = db.OAuthAccounts
                               .Where(a => a.TaiKhoanID == tk.TaiKhoanID)
                               .OrderByDescending(a => a.LinkedAt)
                               .FirstOrDefault();

                    if (oa != null)
                    {
                        if (!string.IsNullOrWhiteSpace(oa.FullName)) return oa.FullName;
                        if (!string.IsNullOrWhiteSpace(oa.Email)) return oa.Email;
                    }
                }
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(tk.Email)) return tk.Email;

            var u = tk.Username ?? "";
            const string fbPrefix = "Facebook:";
            if (u.StartsWith(fbPrefix, StringComparison.OrdinalIgnoreCase))
                return u.Substring(fbPrefix.Length);

            return u.Length > 0 ? u : "(unknown)";
        }

        private void Navigate(UserControl uc)
        {
            var shell = Window.GetWindow(this) as DashboardWindow;
            if (shell?.MainContent != null) shell.MainContent.Content = uc;
            else MessageBox.Show("Không tìm thấy vùng nội dung để điều hướng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void BtnAI_Click(object sender, RoutedEventArgs e)
        {
            var win = new AIWindow(currentUser);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        // ==== Phản ánh / Ticket ====
        private void BtnTicket_Click(object sender, RoutedEventArgs e)
            => Navigate(new TicketControl(currentUser));

        // ==== Nhật ký: Admin + Manager mở được ====
        private void BtnViewLog_Click(object sender, RoutedEventArgs e)
        {
            if (!RoleHelper.IsAdmin(currentUser) && !RoleHelper.IsManager(currentUser))
            {
                MessageBox.Show("Bạn không có quyền xem nhật ký hoạt động.",
                                "Không có quyền",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TRUYỀN currentUser VÀO ĐÂY
            Navigate(new ActivityLogControl(currentUser));
        }

    }
}
