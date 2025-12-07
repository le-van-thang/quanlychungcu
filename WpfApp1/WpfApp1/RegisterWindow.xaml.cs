using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1
{
    public partial class RegisterWindow : Window
    {
        private readonly QuanlychungcuEntities db = new QuanlychungcuEntities();
        private bool _showPwd = false;
        private bool _showConfirm = false;
        private bool _syncingPwd = false;
        private bool _syncingConfirm = false;

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void btnTogglePwd_Click(object sender, RoutedEventArgs e)
        {
            _showPwd = !_showPwd;
            if (_showPwd)
            {
                txtPasswordVisible.Text = pwdPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                pwdPassword.Visibility = Visibility.Collapsed;
                btnTogglePwd.Content = "🙈";
            }
            else
            {
                pwdPassword.Password = txtPasswordVisible.Text;
                pwdPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                btnTogglePwd.Content = "👁";
            }
        }

        private void btnToggleConfirm_Click(object sender, RoutedEventArgs e)
        {
            _showConfirm = !_showConfirm;
            if (_showConfirm)
            {
                txtConfirmVisible.Text = pwdConfirm.Password;
                txtConfirmVisible.Visibility = Visibility.Visible;
                pwdConfirm.Visibility = System.Windows.Visibility.Collapsed;
                btnToggleConfirm.Content = "🙈";
            }
            else
            {
                pwdConfirm.Password = txtConfirmVisible.Text;
                pwdConfirm.Visibility = System.Windows.Visibility.Visible;
                txtConfirmVisible.Visibility = System.Windows.Visibility.Collapsed;
                btnToggleConfirm.Content = "👁";
            }
        }

        private void pwdPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = PasswordStrength(pwdPassword.Password);
            if (_showPwd)
            {
                if (_syncingPwd) return;
                _syncingPwd = true;
                txtPasswordVisible.Text = pwdPassword.Password;
                _syncingPwd = false;
            }
        }

        private void pwdConfirm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var basePwd = _showPwd ? txtPasswordVisible.Text : pwdPassword.Password;
            txtStatus.Text = (!string.IsNullOrEmpty(pwdConfirm.Password) && pwdConfirm.Password != basePwd)
                                ? "Mật khẩu xác nhận chưa khớp" : "";

            if (_showConfirm)
            {
                if (_syncingConfirm) return;
                _syncingConfirm = true;
                txtConfirmVisible.Text = pwdConfirm.Password;
                _syncingConfirm = false;
            }
        }

        private void txtPasswordVisible_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_syncingPwd) return;
            _syncingPwd = true;
            pwdPassword.Password = txtPasswordVisible.Text;
            _syncingPwd = false;
        }

        private void txtConfirmVisible_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_syncingConfirm) return;
            _syncingConfirm = true;
            pwdConfirm.Password = txtConfirmVisible.Text;
            _syncingConfirm = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = (txtEmail.Text ?? "").Trim().ToLowerInvariant();
            string password = _showPwd ? txtPasswordVisible.Text : pwdPassword.Password;
            string confirm = _showConfirm ? txtConfirmVisible.Text : pwdConfirm.Password;

            string msg = ValidateInput(username, email, password, confirm, chkAgree.IsChecked == true);
            if (msg != null)
            {
                MessageBox.Show(msg, "Thiếu/không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chặn trùng username/email
            if (db.TaiKhoans.Any(x => x.Username == username))
            {
                MessageBox.Show("Tên đăng nhập đã tồn tại.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (db.TaiKhoans.Any(x => (x.Email ?? "").ToLower() == email))
            {
                MessageBox.Show("Email đã được đăng ký.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Hash mật khẩu bằng SecureVault (giống Login + ForgotPassword)
            string stored = SecureVault.HashPassword(password);

            var tk = new TaiKhoan
            {
                Username = username,
                PasswordHash = stored,
                Email = email,
                VaiTro = "User",      // người đăng ký mới luôn là User
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.TaiKhoans.Add(tk);
            db.SaveChanges();

            AuditLogger.Log("Register", "TaiKhoan",
                tk.TaiKhoanID.ToString(),
                $"Đăng ký tài khoản mới: {username} ({email})");

            MessageBox.Show("Đăng ký thành công!", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        // ==== Helpers ====
        private static string ValidateInput(string username, string email, string password, string confirm, bool agreed)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 4)
                return "Tên đăng nhập tối thiểu 4 ký tự";
            if (!Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Email không hợp lệ";
            if (string.IsNullOrEmpty(password) || password.Length < 6)
                return "Mật khẩu tối thiểu 6 ký tự";
            if (password != confirm)
                return "Xác nhận mật khẩu không khớp";
            if (!agreed)
                return "Bạn cần đồng ý Điều khoản & Chính sách";
            return null;
        }

        private static string PasswordStrength(string pwd)
        {
            if (string.IsNullOrEmpty(pwd)) return "";
            int s = 0;
            if (pwd.Length >= 8) s++;
            if (Regex.IsMatch(pwd, @"[A-Z]")) s++;
            if (Regex.IsMatch(pwd, @"[a-z]")) s++;
            if (Regex.IsMatch(pwd, @"\d")) s++;                    // ✅ sửa \\d -> \d
            if (Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) s++;
            return s <= 2 ? "Mật khẩu yếu"
                 : s == 3 ? "Mật khẩu trung bình"
                 : s == 4 ? "Mật khẩu khá"
                 : "Mật khẩu mạnh";
        }
    }
}
