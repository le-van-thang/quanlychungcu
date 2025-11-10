using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class CanHoControl : UserControl
    {
        private TaiKhoan _currentUser;

        // Constructor có user (dùng để phân quyền)
        public CanHoControl(TaiKhoan user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadData();
            ApplyRolePermission();
        }

        // Constructor mặc định (nếu muốn gọi mà không cần truyền user)
        public CanHoControl()
        {
            InitializeComponent();
            LoadData();
            ApplyRolePermission();   // thêm dòng này để đồng bộ
        }

        // Ẩn/hiện nút theo quyền
        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            // Hỗ trợ cả hai kiểu tên nút: btnThem/btnSua/btnXoa hoặc btnAdd/btnEdit/btnDelete
            if (FindName("btnThem") is Button them) them.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnSua") is Button sua) sua.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnXoa") is Button xoa) xoa.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnAdd") is Button add) add.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnEdit") is Button edit) edit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnDelete") is Button del) del.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // Load danh sách căn hộ + tìm kiếm nâng cao
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

                dgCanHo.ItemsSource = q
                    .OrderBy(x => x.TenTang)
                    .ThenBy(x => x.SoCanHo)
                    .ToList();
            }
        }

        private CanHoRow Current() => dgCanHo.SelectedItem as CanHoRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadData(txtSearch.Text);

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 căn hộ."); return; }
            var w = new CanHoDetailWindow(row.CanHoID, readOnly: true);
            w.ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var w = new CanHoDetailWindow(null);
            if (w.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 căn hộ cần sửa."); return; }
            var w = new CanHoDetailWindow(row.CanHoID);
            if (w.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 căn hộ cần xóa."); return; }
            if (MessageBox.Show($"Xóa căn {row.SoCanHo}?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.CanHoes.FirstOrDefault(x => x.CanHoID == row.CanHoID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                if (entity.CuDans.Any() || entity.HoaDonCuDans.Any())
                {
                    MessageBox.Show("Căn hộ đang có dữ liệu liên quan (cư dân/hóa đơn). Không thể xóa.");
                    return;
                }

                try
                {
                    db.CanHoes.Remove(entity);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể xóa: " + ex.Message, "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            LoadData(txtSearch.Text);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";  // reset ô tìm kiếm
            LoadData();           // gọi lại hàm load dữ liệu
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
