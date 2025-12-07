using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class CuDanControl : UserControl
    {
        private TaiKhoan _currentUser;

        // Constructor có user (dùng để phân quyền)
        public CuDanControl(TaiKhoan user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadData();
            ApplyRolePermission();
        }

        // Constructor mặc định (nếu gọi không cần user)
        public CuDanControl()
        {
            InitializeComponent();
            LoadData();
            ApplyRolePermission(); // thêm để đảm bảo phân quyền khi không truyền user
        }

        // Ẩn/hiện nút theo quyền
        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            if (FindName("btnThem") is Button them) them.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnSua") is Button sua) sua.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnXoa") is Button xoa) xoa.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnAdd") is Button add) add.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnEdit") is Button edit) edit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnDelete") is Button del) del.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // Load danh sách cư dân
        private void LoadData()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var ds = (from c in db.CuDans
                          join ch in db.CanHoes on c.CanHoID equals ch.CanHoID into gj
                          from ch in gj.DefaultIfEmpty()
                          select new
                          {
                              c.CuDanID,
                              c.HoTen,
                              c.NgaySinh,
                              c.DienThoai,
                              c.Email,
                              SoCanHo = ch != null ? ch.SoCanHo : "(Chưa có)"
                          })
                          .ToList();

                dgCuDan.ItemsSource = ds;
            }
        }

        private void BtnXem_Click(object sender, RoutedEventArgs e)
        {
            if (dgCuDan.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn cư dân để xem!");
                return;
            }

            dynamic row = dgCuDan.SelectedItem;
            int id = row.CuDanID;

            using (var db = new QuanlychungcuEntities())
            {
                var cuDan = db.CuDans.FirstOrDefault(c => c.CuDanID == id);
                if (cuDan != null)
                {
                    var win = new CuDanDetailWindow("View", cuDan);
                    win.Owner = Window.GetWindow(this);
                    win.ShowDialog();

                    // === LOG: xem cư dân ===
                    AuditLogger.Log("View", "CuDan", id.ToString(),
                        $"Xem thông tin cư dân: {cuDan.HoTen} (ID={id})");
                }
            }
        }

        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            var win = new CuDanDetailWindow("Add");
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true)
            {
                LoadData();

                // === LOG: thêm cư dân ===
                AuditLogger.Log("Create", "CuDan", null,
                    "Thêm cư dân mới (qua màn hình chi tiết cư dân)");
            }
        }

        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (dgCuDan.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn cư dân để sửa!");
                return;
            }

            dynamic row = dgCuDan.SelectedItem;
            int id = row.CuDanID;

            using (var db = new QuanlychungcuEntities())
            {
                var cuDan = db.CuDans.FirstOrDefault(c => c.CuDanID == id);
                if (cuDan != null)
                {
                    var win = new CuDanDetailWindow("Edit", cuDan);
                    win.Owner = Window.GetWindow(this);
                    if (win.ShowDialog() == true)
                    {
                        LoadData();

                        // === LOG: sửa cư dân ===
                        AuditLogger.Log("Update", "CuDan", id.ToString(),
                            $"Sửa thông tin cư dân: {cuDan.HoTen} (ID={id})");
                    }
                }
            }
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (dgCuDan.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn cư dân để xóa!");
                return;
            }

            dynamic row = dgCuDan.SelectedItem;
            int id = row.CuDanID;

            if (MessageBox.Show("Bạn có chắc muốn xóa cư dân này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.CuDans.FirstOrDefault(c => c.CuDanID == id);
                if (entity != null)
                {
                    // Kiểm tra dữ liệu liên quan (hóa đơn, xe…)
                    if (entity.HoaDonCuDans.Any() || entity.XeOToes.Any() || entity.XeMays.Any() || entity.XeDaps.Any())
                    {
                        MessageBox.Show("Cư dân đang có dữ liệu liên quan (hóa đơn/xe). Không thể xóa.");
                        return;
                    }

                    try
                    {
                        db.CuDans.Remove(entity);
                        db.SaveChanges();

                        // === LOG: xóa cư dân ===
                        AuditLogger.Log("Delete", "CuDan", id.ToString(),
                            $"Xóa cư dân: {entity.HoTen} (ID={id})");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa: " + ex.Message, "Lỗi",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            LoadData();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearch.Text.Trim().ToLower();

            using (var db = new QuanlychungcuEntities())
            {
                var ds = (from c in db.CuDans
                          join ch in db.CanHoes on c.CanHoID equals ch.CanHoID into gj
                          from ch in gj.DefaultIfEmpty()
                          select new
                          {
                              c.CuDanID,
                              c.HoTen,
                              c.NgaySinh,
                              c.DienThoai,
                              c.Email,
                              SoCanHo = ch != null ? ch.SoCanHo : "(Chưa có)"
                          }).ToList();

                if (!string.IsNullOrEmpty(keyword))
                {
                    ds = ds.Where(x =>
                            (!string.IsNullOrEmpty(x.HoTen) && x.HoTen.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(x.DienThoai) && x.DienThoai.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(x.Email) && x.Email.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(x.SoCanHo) && x.SoCanHo.ToLower().Contains(keyword))
                        ).ToList();
                }

                dgCuDan.ItemsSource = ds;
            }

            // (tuỳ chọn) log tìm kiếm
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                AuditLogger.Log("Search", "CuDan", null,
                    $"Tìm kiếm cư dân với từ khóa: \"{txtSearch.Text}\"");
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";   // reset ô tìm kiếm
            LoadData();            // load lại danh sách cư dân
        }
    }
}
