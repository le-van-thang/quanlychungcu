// TaiKhoanInfoControl.xaml.cs (rút gọn phần liên quan)
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

        // map linh hoạt
        private static readonly string[] Col_FullName = { "HoTen", "HoVaTen", "FullName" };
        private static readonly string[] Col_BirthDate = { "NgaySinh", "BirthDate", "NgaySinhDate" };
        private static readonly string[] Col_Avatar = { "Avatar", "AnhDaiDien", "Photo" };
        private static readonly string[] Col_UserName = { "Username", "UserName", "TenDangNhap" };
        private static readonly string[] Col_PwHash = { "MatKhauHash", "PasswordHash" };

        public TaiKhoanInfoControl(TaiKhoan currentUser)
        {
            InitializeComponent();

            // lấy entity mới nhất theo ID
            _user = _db.TaiKhoans.First(x => x.TaiKhoanID == currentUser.TaiKhoanID);

            LoadUserInfo();
            SetEditMode(false);
        }

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
        }

        private void LoadUserInfo()
        {
            // 1) Họ tên: ưu tiên cột họ tên, nếu không có dùng Username
            var fullName = GetProp<string>(_user, Col_FullName);
            var username = GetProp<string>(_user, Col_UserName) ?? "";
            if (string.IsNullOrWhiteSpace(fullName)) fullName = username;
            txtHoTen.Text = fullName;

            // 2) Ngày sinh (nếu có cột)
            dpNgaySinh.SelectedDate = GetProp<DateTime?>(_user, Col_BirthDate);

            // 3) Ảnh đại diện: DB trước, không có thì lấy ở AvatarStorage
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

            // reset panel đổi mật khẩu
            ckChangePw.IsChecked = false;
            pwPanel.Visibility = Visibility.Collapsed;
            pbOldPw.Password = pbNewPw.Password = pbConfirmPw.Password = "";
        }

        private void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Ảnh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() == true)
            {
                // Đọc bytes ảnh đã chọn
                var bytes = File.ReadAllBytes(dlg.FileName);

                // Thử lưu vào DB nếu có cột Avatar
                if (FindProp(_user, Col_Avatar) != null)
                {
                    SetProp(_user, Col_Avatar, bytes);
                    _db.SaveChanges();
                }
                else
                {
                    // fallback lưu ra ổ đĩa theo Username
                    var username = GetProp<string>(_user, Col_UserName) ?? "unknown";
                    AvatarStorage.Save(username, bytes);
                }

                imgAvatar.Source = ToBitmap(bytes);
                txtInitials.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnRemoveAvatar_Click(object sender, RoutedEventArgs e)
        {
            // DB có cột thì xóa DB; nếu không thì xóa file local
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

        private void BtnEdit_Click(object sender, RoutedEventArgs e) => SetEditMode(true);

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _db.Entry(_user).Reload();
            LoadUserInfo();
            SetEditMode(false);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // họ tên/ngày sinh chỉ lưu nếu DB có cột tương ứng
                if (FindProp(_user, Col_FullName) != null)
                    SetProp(_user, Col_FullName, (txtHoTen.Text ?? "").Trim());

                if (FindProp(_user, Col_BirthDate) != null)
                    SetProp(_user, Col_BirthDate, dpNgaySinh.SelectedDate);

                if (ckChangePw.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(pbOldPw.Password) ||
                        string.IsNullOrWhiteSpace(pbNewPw.Password) ||
                        string.IsNullOrWhiteSpace(pbConfirmPw.Password))
                    {
                        MessageBox.Show("Vui lòng nhập đủ ba ô mật khẩu.");
                        return;
                    }
                    if (pbNewPw.Password != pbConfirmPw.Password)
                    {
                        MessageBox.Show("Xác nhận mật khẩu không khớp.");
                        return;
                    }

                    var hash = GetProp<string>(_user, Col_PwHash) ?? "";
                    if (!SecureVault.VerifyPassword(pbOldPw.Password, hash))
                    {
                        MessageBox.Show("Mật khẩu hiện tại không đúng.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var newHash = SecureVault.HashPassword(pbNewPw.Password);
                    SetProp(_user, Col_PwHash, newHash);
                }

                _db.SaveChanges();
                MessageBox.Show("Đã cập nhật tài khoản.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SetEditMode(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu không thành công:\n" + ex.Message);
            }
        }

        // ---------- helpers ----------
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
                var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
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
            var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
            return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
        }

        private void ckChangePw_Checked(object sender, RoutedEventArgs e)
        {
            pwPanel.Visibility = Visibility.Visible;
        }

        private void ckChangePw_Unchecked(object sender, RoutedEventArgs e)
        {
            pwPanel.Visibility = Visibility.Collapsed;
            pbOldPw.Password = pbNewPw.Password = pbConfirmPw.Password = "";
        }
    }
}
