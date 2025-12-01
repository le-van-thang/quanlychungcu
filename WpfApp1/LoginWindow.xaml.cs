using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using WpfApp1.Services;
using WpfApp1.Windows;

namespace WpfApp1
{
    public partial class LoginWindow : Window
    {
        private bool _showing = false;

        public LoginWindow()
        {
            InitializeComponent();

            // Load "Nhớ tài khoản"
            var rem = RememberStore.Load();
            if (rem != null)
            {
                txtUsername.Text = rem.Value.user ?? "";
                txtPassword.Password = rem.Value.pass ?? "";
                chkRemember.IsChecked = true;
                if (txtPassword.Password.Length > 0)
                    txtPasswordPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var user = txtUsername.Text.Trim();
            var pass = (_showing ? txtPasswordVisible.Text : txtPassword.Password).Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new QuanlychungcuEntities())
                {
                    var tk = db.TaiKhoans.FirstOrDefault(t => t.Username == user);
                    if (tk == null)
                    {
                        AuditLogger.LogLogin(user, false, "Không tìm thấy tài khoản");
                        MessageBox.Show("Không tìm thấy tài khoản này.", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Tài khoản MXH / không có mật khẩu nội bộ
                    if (string.IsNullOrEmpty(tk.PasswordHash) ||
                        string.Equals(tk.PasswordHash, "EXTERNAL_LOGIN", StringComparison.OrdinalIgnoreCase))
                    {
                        AuditLogger.LogLogin(user, false, "Tài khoản external (Google/Facebook) không dùng mật khẩu cục bộ");
                        MessageBox.Show("Tài khoản này đăng nhập bằng Google / Facebook, không dùng mật khẩu.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // ✅ Dùng SecureVault để verify password đã hash (50000....)
                    if (!SecureVault.VerifyPassword(pass, tk.PasswordHash))
                    {
                        AuditLogger.LogLogin(user, false, "Sai mật khẩu");
                        MessageBox.Show("Mật khẩu không đúng!", "Sai mật khẩu",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Kiểm tra IsActive
                    if (TryGetBool(tk, "IsActive") == false)
                    {
                        AuditLogger.LogLogin(user, false, "Tài khoản bị khóa");
                        MessageBox.Show("Tài khoản đã bị khóa.", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Nhớ tài khoản
                    if (chkRemember.IsChecked == true) RememberStore.Save(user, pass);
                    else RememberStore.Clear();

                    // Lưu session
                    SessionStore.Save(tk.TaiKhoanID);

                    AuditLogger.LogLogin(user, true, $"Đăng nhập thành công (ID={tk.TaiKhoanID})");

                    MessageBox.Show("Đăng nhập thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    var main = new DashboardWindow(tk);
                    main.Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                AuditLogger.Log("LoginError", "Auth", null, ex.ToString());
                MessageBox.Show("Không đăng nhập được:\n" + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string email = AskEmail();
                if (string.IsNullOrEmpty(email)) return;

                using (var db = new QuanlychungcuEntities())
                {
                    var tk = db.TaiKhoans.FirstOrDefault(x => (x.Email ?? "").ToLower() == email);
                    if (tk == null)
                    {
                        MessageBox.Show("Email này chưa có trong hệ thống.", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrEmpty(tk.PasswordHash) ||
                        string.Equals(tk.PasswordHash, "EXTERNAL_LOGIN", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Tài khoản đăng nhập MXH nên không thể đặt lại mật khẩu.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var key = "reset:" + email;
                    var otp = OtpStore.Issue(key, TimeSpan.FromMinutes(5));

                    try
                    {
                        await Mailer.SendAsync(email, "Xác nhận đặt lại mật khẩu",
$@"Xin chào,

Mã OTP của bạn là: {otp}
Mã có hiệu lực trong 5 phút.
Vui lòng không chia sẻ mã này cho bất kỳ ai.

Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.");
                    }
                    catch (Exception exMail)
                    {
                        MessageBox.Show("Không gửi được email:\n" + exMail.Message,
                            "Lỗi Email", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    AuditLogger.Log("ForgotPassword", "Auth", tk.TaiKhoanID.ToString(),
                        "Gửi OTP đặt lại mật khẩu");

                    MessageBox.Show("Mã OTP đã được gửi. Vui lòng kiểm tra email.",
                        "Đã gửi OTP", MessageBoxButton.OK, MessageBoxImage.Information);

                    bool otpOk = AskOtp(key, 3);
                    if (!otpOk) return;

                    var newPwd1 = InputBox.Show("Nhập mật khẩu mới:", "Đặt lại mật khẩu");
                    if (string.IsNullOrWhiteSpace(newPwd1)) return;
                    var newPwd2 = InputBox.Show("Nhập lại mật khẩu mới:", "Xác nhận mật khẩu");
                    if (newPwd1 != newPwd2)
                    {
                        MessageBox.Show("Hai lần nhập không khớp.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // ✅ Lưu lại bằng SecureVault giống màn Tài khoản
                    tk.PasswordHash = SecureVault.HashPassword(newPwd1);
                    db.SaveChanges();

                    AuditLogger.Log("ResetPassword", "Auth", tk.TaiKhoanID.ToString(),
                        "Đặt lại mật khẩu qua OTP");

                    MessageBox.Show("Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại!",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                AuditLogger.Log("ResetPasswordError", "Auth", null, ex.ToString());
                MessageBox.Show("Có lỗi xảy ra:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var reg = new RegisterWindow();
            reg.ShowDialog();
        }

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!_showing)
            {
                _showing = true;
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                btnTogglePassword.Content = "🙈";
                txtPasswordPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                _showing = false;
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                btnTogglePassword.Content = "👁";
                if (txtPassword.Password.Length == 0)
                    txtPasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Password.Length > 0)
                txtPasswordPlaceholder.Visibility = Visibility.Collapsed;
            else if (txtPassword.Visibility == Visibility.Visible)
                txtPasswordPlaceholder.Visibility = Visibility.Visible;
        }

        // ================== helper ==================

        private string AskEmail()
        {
            while (true)
            {
                var dlg = new InputDialog("Nhập email đã đăng ký để nhận mã OTP:", "Quên mật khẩu");
                bool? ok = dlg.ShowDialog();
                if (ok != true) return null;

                var email = (dlg.Result ?? "").Trim().ToLowerInvariant();
                if (email.Length == 0)
                {
                    MessageBox.Show("Vui lòng nhập email.", "Thiếu dữ liệu",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Email không hợp lệ.", "Sai định dạng",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }
                return email;
            }
        }

        private bool AskOtp(string key, int maxAttempts = 3)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var dlg = new InputDialog("Nhập mã OTP vừa nhận email:", "Xác minh OTP");
                bool? ok = dlg.ShowDialog();
                if (ok != true) return false;

                var otpInput = (dlg.Result ?? "").Trim();
                if (otpInput.Length == 0)
                {
                    MessageBox.Show("Vui lòng nhập OTP.", "Thiếu dữ liệu",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                if (OtpStore.Verify(key, otpInput)) return true;

                MessageBox.Show("OTP sai hoặc đã hết hạn.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private static bool? TryGetBool(object obj, string propName)
        {
            var pi = obj.GetType().GetProperty(propName);
            if (pi == null) return null;
            var v = pi.GetValue(obj);
            if (v == null) return null;
            if (v is bool b) return b;
            bool r; if (bool.TryParse(v.ToString(), out r)) return r;
            return null;
        }

        // ============ Google / Facebook ============

        private async void BtnLoginGoogle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var svc = new ExternalAuthService();
                var u = await svc.SignInWithGoogleAsync();

                MessageBox.Show($"Google xác thực OK!\nEmail: {u.Email}\nTên: {u.FullName}",
                                "Đăng nhập Google", MessageBoxButton.OK, MessageBoxImage.Information);

                using (var db = new QuanlychungcuEntities())
                {
                    var username = (u.Email ?? "").Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(username))
                        username = "Google:" + (u.ProviderUserId ?? Guid.NewGuid().ToString("N"));

                    var tk = db.TaiKhoans
                               .FirstOrDefault(x => (x.Email ?? "") == u.Email || (x.Username ?? "") == username);

                    if (tk == null)
                    {
                        tk = new TaiKhoan
                        {
                            Username = username,
                            Email = u.Email,
                            PasswordHash = "EXTERNAL_LOGIN",
                            VaiTro = "User",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        db.TaiKhoans.Add(tk);
                        db.SaveChanges();
                    }

                    await svc.UpsertExternalUserAsync(u, "User");

                    SessionStore.Save(tk.TaiKhoanID);

                    AuditLogger.LogLogin(username, true, "Đăng nhập bằng Google");

                    MessageBox.Show("Đăng nhập Google thành công! Chuyển vào Trang chủ.",
                                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    var main = new DashboardWindow(tk);
                    main.Show();
                    Close();
                }
            }
            catch (TokenResponseException trex)
            {
                AuditLogger.Log("LoginError", "Auth", null, "Google TokenResponseException: " +
                    (trex.Error?.ErrorDescription ?? trex.Message));

                MessageBox.Show("Đổi code lấy token thất bại: " +
                    (trex.Error?.ErrorDescription ?? trex.Message),
                    "Google OAuth", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message
                          ?? ex.InnerException?.Message
                          ?? ex.Message;

                AuditLogger.Log("LoginError", "Auth", null, "Google login error: " + msg);

                MessageBox.Show("Google login error (detail): " + msg, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnLoginFacebook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new FacebookLoginWindow { Owner = this };
                if (win.ShowDialog() != true || string.IsNullOrEmpty(win.AccessToken))
                {
                    MessageBox.Show("Bạn đã hủy đăng nhập Facebook.", "Thông báo",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var svc = new ExternalAuthService();
                var u = await svc.GetFacebookUserAsync(win.AccessToken);

                var tk = await svc.UpsertExternalUserAsync(u, "User");

                SessionStore.Save(tk.TaiKhoanID);

                AuditLogger.LogLogin(tk.Username ?? ("Facebook:" + u.ProviderUserId),
                    true, "Đăng nhập bằng Facebook");

                MessageBox.Show("Đăng nhập Facebook thành công! Chuyển vào Trang chủ.",
                                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                var main = new DashboardWindow(tk);
                main.Show();
                Close();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var msg = ex.InnerException?.InnerException?.Message
                       ?? ex.InnerException?.Message
                       ?? ex.Message;

                AuditLogger.Log("LoginError", "Auth", null, "Facebook DbUpdateException: " + msg);

                MessageBox.Show("Facebook login error (DbUpdate): " + msg,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message
                       ?? ex.InnerException?.Message
                       ?? ex.Message;

                AuditLogger.Log("LoginError", "Auth", null, "Facebook login error: " + msg);

                MessageBox.Show("Facebook login error (detail): " + msg,
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
