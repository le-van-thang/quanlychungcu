using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class CanHoControl : UserControl
    {
        private TaiKhoan _currentUser;

        // Constructor mặc định – Dashboard gọi cái này
        public CanHoControl()
        {
            InitializeComponent();
            LoadData();
        }

        // Constructor có user (để phân quyền)
        public CanHoControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            ApplyRolePermission();
        }

        // Ẩn / hiện nút theo quyền
        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            if (FindName("btnThem") is Button them)
                them.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnSua") is Button sua)
                sua.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnXoa") is Button xoa)
                xoa.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Dự phòng nếu sau này đổi tên nút
            if (FindName("btnAdd") is Button add)
                add.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnEdit") is Button edit)
                edit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnDelete") is Button del)
                del.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // Load danh sách căn hộ + search
        private void LoadData(string keyword = null)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = db.CanHoes.Select(ch => new CanHoRow
                {
                    CanHoID = ch.CanHoID,
                    SoCanHo = ch.SoCanHo,
                    DienTich = ch.DienTich,
                    GiaTri = ch.GiaTri,
                    TenTang = ch.Tang != null ? ch.Tang.TenTang : null,
                    SoCuDan = ch.CuDans.Count()
                });

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower();
                    q = q.Where(x =>
                        (x.SoCanHo ?? "").ToLower().Contains(keyword) ||
                        (x.TenTang ?? "").ToLower().Contains(keyword) ||
                        (x.DienTich != null && x.DienTich.ToString().Contains(keyword)) ||
                        (x.GiaTri != null && x.GiaTri.ToString().Contains(keyword)) ||
                        x.SoCuDan.ToString().Contains(keyword)
                    );
                }

                dgCanHoGrid.ItemsSource = q
                    .OrderBy(x => x.TenTang)
                    .ThenBy(x => x.SoCanHo)
                    .ToList();
            }
        }

        private CanHoRow Current() => dgCanHoGrid.SelectedItem as CanHoRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadData(txtCanHoSearch.Text);

            if (!string.IsNullOrWhiteSpace(txtCanHoSearch.Text))
            {
                AuditLogger.Log("Search", "CanHo", null,
                    $"Tìm kiếm căn hộ với từ khóa: \"{txtCanHoSearch.Text}\"");
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn 1 căn hộ.");
                return;
            }

            var w = new CanHoDetailWindow(row.CanHoID, readOnly: true);
            w.Owner = Window.GetWindow(this);
            w.ShowDialog();

            // === LOG: xem căn hộ ===
            AuditLogger.Log("View", "CanHo", row.CanHoID.ToString(),
                $"Xem thông tin căn hộ {row.SoCanHo} (ID={row.CanHoID})");
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var w = new CanHoDetailWindow(null);
            w.Owner = Window.GetWindow(this);
            if (w.ShowDialog() == true)
            {
                LoadData(txtCanHoSearch.Text);

                // === LOG: thêm căn hộ ===
                AuditLogger.Log("Create", "CanHo", null,
                    "Thêm căn hộ mới (qua màn hình chi tiết căn hộ)");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn 1 căn hộ cần sửa.");
                return;
            }

            var w = new CanHoDetailWindow(row.CanHoID);
            w.Owner = Window.GetWindow(this);
            if (w.ShowDialog() == true)
            {
                LoadData(txtCanHoSearch.Text);

                // === LOG: sửa căn hộ ===
                AuditLogger.Log("Update", "CanHo", row.CanHoID.ToString(),
                    $"Sửa thông tin căn hộ {row.SoCanHo} (ID={row.CanHoID})");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn 1 căn hộ cần xóa.");
                return;
            }

            if (MessageBox.Show($"Bạn có chắc muốn xóa căn {row.SoCanHo}?",
                    "Xác nhận", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.CanHoes.FirstOrDefault(x => x.CanHoID == row.CanHoID);
                if (entity == null)
                {
                    MessageBox.Show("Không tìm thấy bản ghi.");
                    return;
                }

                if (entity.CuDans.Any() || entity.HoaDonCuDans.Any())
                {
                    MessageBox.Show("Căn hộ đang có dữ liệu liên quan (cư dân / hóa đơn). Không thể xóa.");
                    return;
                }

                try
                {
                    db.CanHoes.Remove(entity);
                    db.SaveChanges();

                    // === LOG: xóa căn hộ ===
                    AuditLogger.Log("Delete", "CanHo", row.CanHoID.ToString(),
                        $"Xóa căn hộ {row.SoCanHo} (ID={row.CanHoID})");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể xóa: " + ex.Message, "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            LoadData(txtCanHoSearch.Text);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtCanHoSearch.Text = "";
            LoadData();
        }
    }

    public class CanHoRow
    {
        public int CanHoID { get; set; }
        public string SoCanHo { get; set; }
        public decimal? DienTich { get; set; }
        public decimal? GiaTri { get; set; }
        public string TenTang { get; set; }
        public int SoCuDan { get; set; }
    }
}
