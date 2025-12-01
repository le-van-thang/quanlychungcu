using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class TaiKhoanInfoControl : UserControl
    {
        private readonly QuanlychungcuEntities _db = new QuanlychungcuEntities();
        private TaiKhoan _user;

        // Các tên cột có thể có trong DB (để map linh hoạt)
        private static readonly string[] Col_FullName = { "HoTen", "HoVaTen", "FullName" };
        private static readonly string[] Col_BirthDate = { "NgaySinh", "BirthDate", "NgaySinhDate" };
        private static readonly string[] Col_Avatar = { "Avatar", "AnhDaiDien", "Photo" };
        private static readonly string[] Col_UserName = { "Username", "UserName", "TenDangNhap" };
        private static readonly string[] Col_Email = { "Email", "Mail" };
        private static readonly string[] Col_Role = { "VaiTro", "Role", "Quyen" };
        private static readonly string[] Col_PwHash = { "MatKhauHash", "PasswordHash" };

        public TaiKhoanInfoControl(TaiKhoan currentUser)
        {
            InitializeComponent();

            // lấy entity mới nhất theo ID
            _user = _db.TaiKhoans.First(x => x.TaiKhoanID == currentUser.TaiKhoanID);

            LoadUserInfo();
            SetEditMode(false);
        }

        // ================== UI MODE ==================

        private void SetEditMode(bool isEdit)
        {
            txtHoTen.IsReadOnly = !isEdit;
            dpNgaySinh.IsEnabled = isEdit;
            ckChangePw.IsEnabled = isEdit;

            btnEdit.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            btnSave.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            btnChangeAvatar.IsEnabled = isEdit;
            btnRemoveAvatar.IsEnabled = isEdit;

            if (!isEdit)
            {
                ckChangePw.IsChecked = false;
                pwPanel.Visibility = Visibility.Collapsed;
                pbOldPw.Password = pbNewPw.Password = pbConfirmPw.Password = "";
            }
        }

        // ================== LOAD DATA ==================

        private void LoadUserInfo()
        {
            // Họ tên
            var fullName = GetProp<string>(_user, Col_FullName);
            var username = GetProp<string>(_user, Col_UserName) ?? "";
            if (string.IsNullOrWhiteSpace(fullName)) fullName = username;
            txtHoTen.Text = fullName;

            // Email & role & username hiển thị
            var email = GetProp<string>(_user, Col_Email) ?? "(chưa có email)";
            txtEmail.Text = email;

            var role = GetProp<string>(_user, Col_Role) ?? "User";
            txtRole.Text = $"Vai trò: {role}";

            txtUserName.Text = string.IsNullOrWhiteSpace(username)
                ? ""
                : $"Tên đăng nhập: {username}";

            // Ngày sinh
            dpNgaySinh.SelectedDate = GetProp<DateTime?>(_user, Col_BirthDate);

            // Avatar: ưu tiên DB, không có thì lấy từ AvatarStorage
            byte[] avatarBytes = GetProp<byte[]>(_user, Col_Avatar);
            if (avatarBytes == null || avatarBytes.Length == 0)
                avatarBytes = AvatarStorage.Load(username);

            if (avatarBytes != null && avatarBytes.Length > 0)
            {
                imgAvatar.Source = ToBitmap(avatarBytes);
                txtInitials.Visibility = Visibility.Collapsed;
            }
            else
            {
                imgAvatar.Source = null;
                txtInitials.Text = Initials(txtHoTen.Text);
                txtInitials.Visibility = Visibility.Visible;
            }

            // cảnh báo nếu DB không có cột tên/ngày sinh
            bool hasNameCol = FindProp(_user, Col_FullName) != null;
            bool hasDobCol = FindProp(_user, Col_BirthDate) != null;
            txtWarningReadonly.Visibility = (!hasNameCol || !hasDobCol)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ================== AVATAR ==================

        private void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Ảnh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var bytes = File.ReadAllBytes(dlg.FileName);

                if (FindProp(_user, Col_Avatar) != null)
                {
                    SetProp(_user, Col_Avatar, bytes);
                    _db.SaveChanges();
                }
                else
                {
                    var username = GetProp<string>(_user, Col_UserName) ?? "unknown";
                    AvatarStorage.Save(username, bytes);
                }

                imgAvatar.Source = ToBitmap(bytes);
                txtInitials.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu ảnh đại diện:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRemoveAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FindProp(_user, Col_Avatar) != null)
                {
                    SetProp(_user, Col_Avatar, null);
                    _db.SaveChanges();
                }
                else
                {
                    var username = GetProp<string>(_user, Col_UserName) ?? "unknown";
                    AvatarStorage.Delete(username);
                }

                imgAvatar.Source = null;
                txtInitials.Text = Initials(txtHoTen.Text);
                txtInitials.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể xóa ảnh đại diện:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================== BUTTONS ==================

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // reload entity từ DB để hủy mọi chỉnh sửa
            _db.Entry(_user).Reload();
            LoadUserInfo();
            SetEditMode(false);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Họ tên
                if (FindProp(_user, Col_FullName) != null)
                    SetProp(_user, Col_FullName, (txtHoTen.Text ?? "").Trim());

                // Ngày sinh
                if (FindProp(_user, Col_BirthDate) != null)
                    SetProp(_user, Col_BirthDate, dpNgaySinh.SelectedDate);

                // Đổi mật khẩu (nếu bật)
                if (ckChangePw.IsChecked == true)
                {
                    if (FindProp(_user, Col_PwHash) == null)
                    {
                        MessageBox.Show(
                            "Tài khoản này đăng nhập qua Google/Facebook hoặc không lưu mật khẩu cục bộ, nên không thể đổi mật khẩu ở đây.",
                            "Không hỗ trợ",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(pbOldPw.Password) ||
                        string.IsNullOrWhiteSpace(pbNewPw.Password) ||
                        string.IsNullOrWhiteSpace(pbConfirmPw.Password))
                    {
                        MessageBox.Show("Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.",
                            "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (pbNewPw.Password != pbConfirmPw.Password)
                    {
                        MessageBox.Show("Xác nhận mật khẩu mới không khớp.",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var hash = GetProp<string>(_user, Col_PwHash) ?? "";
                    if (!SecureVault.VerifyPassword(pbOldPw.Password, hash))
                    {
                        MessageBox.Show("Mật khẩu hiện tại không đúng.",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var newHash = SecureVault.HashPassword(pbNewPw.Password);
                    SetProp(_user, Col_PwHash, newHash);
                }

                _db.SaveChanges();
                MessageBox.Show("Đã cập nhật thông tin tài khoản.",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                SetEditMode(false);
                LoadUserInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu không thành công:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================== PASSWORD PANEL ==================

        private void ckChangePw_Checked(object sender, RoutedEventArgs e)
        {
            pwPanel.Visibility = Visibility.Visible;
        }

        private void ckChangePw_Unchecked(object sender, RoutedEventArgs e)
        {
            pwPanel.Visibility = Visibility.Collapsed;
            pbOldPw.Password = pbNewPw.Password = pbConfirmPw.Password = "";
        }

        // ================== HELPERS ==================

        private static T GetProp<T>(object obj, string[] candidates)
        {
            var pi = FindProp(obj, candidates);
            if (pi == null) return default(T);
            var val = pi.GetValue(obj);
            if (val == null) return default(T);
            return (T)ConvertTo(typeof(T), val);
        }

        private static void SetProp(object obj, string[] candidates, object value)
        {
            var pi = FindProp(obj, candidates);
            if (pi == null) return;

            var target = value == null ? null : ConvertTo(pi.PropertyType, value);
            pi.SetValue(obj, target);
        }

        private static PropertyInfo FindProp(object obj, string[] candidates)
        {
            var t = obj.GetType();
            foreach (var name in candidates)
            {
                var pi = t.GetProperty(name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null) return pi;
            }
            return null;
        }

        private static object ConvertTo(Type targetType, object value)
        {
            if (value == null) return null;
            var u = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (u == typeof(byte[]) && value is byte[]) return value;
            if (u.IsEnum) return Enum.Parse(u, value.ToString());
            return System.Convert.ChangeType(value, u);
        }

        private static BitmapImage ToBitmap(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }

        private static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "NA";
            var parts = name.Trim()
                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpper();
            return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
        }
    }
}
