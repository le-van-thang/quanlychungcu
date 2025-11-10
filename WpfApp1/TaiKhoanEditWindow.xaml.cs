using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1
{
    public partial class TaiKhoanEditWindow : Window
    {
        private readonly int? _id;      // null = thêm mới, có giá trị = sửa
        private readonly bool _readOnly;

        public TaiKhoanEditWindow(int? taiKhoanId, bool readOnly = false)
        {
            InitializeComponent();

            _id = taiKhoanId;
            _readOnly = readOnly;

            if (_id == null)
            {
                lblTitle.Text = "Thêm tài khoản";
                chkActive.IsChecked = true;
                cboVaiTro.SelectedIndex = 0; // mặc định User
            }
            else
            {
                lblTitle.Text = _readOnly ? "Xem tài khoản" : "Sửa tài khoản";
                LoadData(_id.Value);
            }

            if (_readOnly)
            {
                txtUsername.IsReadOnly = true;
                txtHoTen.IsReadOnly = true;
                txtEmail.IsReadOnly = true;
                cboVaiTro.IsEnabled = false;
                chkActive.IsEnabled = false;
                pwdNew.IsEnabled = false;
                pwdRe.IsEnabled = false;
                btnSave.IsEnabled = false;
            }
        }

        private void LoadData(int id)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var tk = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanID == id);
                if (tk == null)
                {
                    MessageBox.Show("Không tìm thấy tài khoản.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                    return;
                }

                var prof = db.Users.FirstOrDefault(u => u.TaiKhoanID == tk.TaiKhoanID);

                txtUsername.Text = tk.Username ?? "";
                txtHoTen.Text = prof?.FullName ?? "";
                txtEmail.Text = tk.Email ?? "";

                // chọn vai trò khớp
                SelectRoleInCombo(tk.VaiTro);

                chkActive.IsChecked = tk.IsActive;

                // Khi sửa tránh đổi username (unique)
                txtUsername.IsReadOnly = true;
            }
        }

        private void SelectRoleInCombo(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                cboVaiTro.SelectedIndex = 0;
                return;
            }

            foreach (var it in cboVaiTro.Items)
            {
                var cbi = it as System.Windows.Controls.ComboBoxItem;
                if (string.Equals(cbi?.Content?.ToString(), role, StringComparison.OrdinalIgnoreCase))
                {
                    cboVaiTro.SelectedItem = it;
                    return;
                }
            }
            cboVaiTro.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var username = (txtUsername.Text ?? "").Trim();
                var email = TrimToNull(txtEmail.Text);
                var hoten = TrimToNull(txtHoTen.Text);
                var role = (((System.Windows.Controls.ComboBoxItem)cboVaiTro.SelectedItem)?.Content?.ToString() ?? "User").Trim();
                var active = chkActive.IsChecked == true;

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show("Vui lòng nhập Username.", "Thiếu dữ liệu",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Email không hợp lệ.", "Sai định dạng",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // kiểm tra mật khẩu (nếu user nhập)
                string newPwd = null;
                if (!string.IsNullOrEmpty(pwdNew.Password) || !string.IsNullOrEmpty(pwdRe.Password))
                {
                    if (pwdNew.Password != pwdRe.Password)
                    {
                        MessageBox.Show("Hai mật khẩu không khớp.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    newPwd = pwdNew.Password;
                }

                using (var db = new QuanlychungcuEntities())
                {
                    if (_id == null) // =========== THÊM ===========
                    {
                        // unique username
                        if (db.TaiKhoans.Any(t => t.Username == username))
                        {
                            MessageBox.Show("Username đã tồn tại.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var tk = new TaiKhoan
                        {
                            Username = username,
                            Email = email,
                            PasswordHash = string.IsNullOrEmpty(newPwd)
                                           ? "EXTERNAL_LOGIN" // nếu không nhập mật khẩu
                                           : SecureVault.HashPassword(newPwd),
                            VaiTro = string.IsNullOrWhiteSpace(role) ? "User" : role,
                            IsActive = active,
                            CreatedAt = DateTime.Now,
                            SecurityStamp = Guid.NewGuid().ToString("N")
                        };
                        db.TaiKhoans.Add(tk);
                        db.SaveChanges();

                        // tạo profile [User]
                        var prof = new User
                        {
                            TaiKhoanID = tk.TaiKhoanID,
                            FullName = hoten,
                            Email = email,
                            UserType = MapUserTypeFromRole(role),
                            RefId = null
                        };
                        db.Users.Add(prof);
                        db.SaveChanges();

                        DialogResult = true;
                        Close();
                    }
                    else // =========== SỬA ===========
                    {
                        var tk = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanID == _id.Value);
                        if (tk == null)
                        {
                            MessageBox.Show("Không tìm thấy tài khoản.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // tk.Username để nguyên (đang readonly)
                        tk.Email = email;
                        tk.VaiTro = string.IsNullOrWhiteSpace(role) ? "User" : role;
                        tk.IsActive = active;

                        if (!string.IsNullOrEmpty(newPwd))
                            tk.PasswordHash = SecureVault.HashPassword(newPwd);

                        var prof = db.Users.FirstOrDefault(u => u.TaiKhoanID == tk.TaiKhoanID);
                        if (prof == null)
                        {
                            prof = new User { TaiKhoanID = tk.TaiKhoanID };
                            db.Users.Add(prof);
                        }
                        prof.FullName = hoten;
                        prof.Email = email;
                        prof.UserType = MapUserTypeFromRole(role);

                        db.SaveChanges();
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message
                          ?? ex.InnerException?.Message
                          ?? ex.Message;
                MessageBox.Show("Không lưu được:\n" + msg, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string TrimToNull(string s)
        {
            var t = (s ?? "").Trim();
            return t.Length == 0 ? null : t;
        }

        private static string MapUserTypeFromRole(string role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
            if (string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase)) return "NhanVien";
            return "CuDan";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
