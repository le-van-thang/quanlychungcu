using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
// alias tránh nhầm lẫn với UserControl
using DbUser = WpfApp1.User;

namespace WpfApp1
{
    public partial class TaiKhoanListControl : UserControl
    {
        private TaiKhoan _currentUser;
        private bool _isAdmin;

        public TaiKhoanListControl()
        {
            InitializeComponent();
            LoadGrid();
        }

        // Constructor dùng khi mở từ DashboardWindow
        public TaiKhoanListControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            _isAdmin = RoleHelper.IsAdmin(user);

            if (!_isAdmin)
            {
                MessageBox.Show("Chỉ Admin mới được xem và quản lý danh sách tài khoản.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Khoá hết control cho chắc
                this.IsEnabled = false;
            }
        }

        // Dòng hiển thị trên lưới
        private class AccountRow
        {
            public int TaiKhoanID { get; set; }
            public string Username { get; set; }
            public string HoTen { get; set; }
            public string Email { get; set; }
            public string VaiTro { get; set; }
            public string TrangThai { get; set; }
            public bool IsActive { get; set; } // để sửa nhanh
        }

        private void LoadGrid(string keyword = null)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = from t in db.TaiKhoans
                        join u in db.Users on t.TaiKhoanID equals u.TaiKhoanID into gj
                        from u in gj.DefaultIfEmpty()
                        select new AccountRow
                        {
                            TaiKhoanID = t.TaiKhoanID,
                            Username = t.Username,
                            HoTen = u != null ? u.FullName : null,
                            Email = t.Email,
                            VaiTro = t.VaiTro,
                            IsActive = t.IsActive,
                            TrangThai = t.IsActive ? "Hoạt động" : "Khóa"
                        };

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var k = keyword.Trim().ToLower();
                    q = q.Where(r =>
                        (r.Username ?? "").ToLower().Contains(k) ||
                        (r.Email ?? "").ToLower().Contains(k) ||
                        (r.HoTen ?? "").ToLower().Contains(k) ||
                        (r.VaiTro ?? "").ToLower().Contains(k));
                }

                dgTaiKhoan.ItemsSource = q
                    .OrderBy(r => r.TaiKhoanID)
                    .ToList();
            }
        }

        private AccountRow GetSelectedRow()
        {
            return dgTaiKhoan.SelectedItem as AccountRow;
        }

        // --- Buttons ---

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin) return;
            LoadGrid(txtSearch.Text);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin) return;
            txtSearch.Text = "";
            LoadGrid();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin) return;

            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // mở dialog xem ở chế độ đọc (IsReadOnly=true)
            var win = new TaiKhoanEditWindow(row.TaiKhoanID, readOnly: true);
            win.Owner = Application.Current?.MainWindow;
            win.ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                MessageBox.Show("Chỉ Admin mới được thêm tài khoản.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new TaiKhoanEditWindow(null);
            win.Owner = Application.Current?.MainWindow;
            if (win.ShowDialog() == true)
            {
                LoadGrid(txtSearch.Text);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                MessageBox.Show("Chỉ Admin mới được sửa tài khoản.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new TaiKhoanEditWindow(row.TaiKhoanID);
            win.Owner = Application.Current?.MainWindow;
            if (win.ShowDialog() == true)
            {
                LoadGrid(txtSearch.Text);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                MessageBox.Show("Chỉ Admin mới được xóa tài khoản.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                    $"Bạn có chắc muốn xóa tài khoản '{row.Username}' (ID={row.TaiKhoanID})?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new QuanlychungcuEntities())
                {
                    var tk = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanID == row.TaiKhoanID);
                    if (tk == null)
                    {
                        MessageBox.Show("Tài khoản không còn tồn tại.", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Xóa profile nếu có
                    var prof = db.Users.FirstOrDefault(u => u.TaiKhoanID == tk.TaiKhoanID);
                    if (prof != null) db.Users.Remove(prof);

                    db.TaiKhoans.Remove(tk);
                    db.SaveChanges();
                }

                LoadGrid(txtSearch.Text);
            }
            catch (Exception ex)
            {
                // thường do FK OAuthAccount, LoginAudit...
                var msg = ex.InnerException?.InnerException?.Message
                          ?? ex.InnerException?.Message
                          ?? ex.Message;
                MessageBox.Show("Không thể xóa tài khoản (có thể đang được tham chiếu):\n" + msg,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
