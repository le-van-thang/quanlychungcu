using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class HoaDonCuDanControl : UserControl
    {
        private TaiKhoan _currentUser;

        public HoaDonCuDanControl(TaiKhoan user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadData();
            ApplyRolePermission();
        }

        public HoaDonCuDanControl()
        {
            InitializeComponent();
            LoadData();
            ApplyRolePermission();
        }

        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            if (FindName("btnAdd") is Button add) add.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnEdit") is Button edit) edit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnDelete") is Button del) del.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadData(string keyword = null)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = db.HoaDonCuDans
                    .Select(h => new
                    {
                        h.HoaDonID,
                        SoCanHo = h.CanHo.SoCanHo,
                        CuDan = h.CuDan.HoTen,
                        h.LoaiDichVu,
                        h.SoTien,
                        h.NgayLap,
                        h.TrangThai
                    });

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower();
                    q = q.Where(x =>
                        (x.SoCanHo ?? "").ToLower().Contains(keyword) ||
                        (x.CuDan ?? "").ToLower().Contains(keyword) ||
                        (x.LoaiDichVu ?? "").ToLower().Contains(keyword) ||
                        x.SoTien.ToString().Contains(keyword) ||
                        (x.TrangThai ?? "").ToLower().Contains(keyword)
                    );
                }

                dgHoaDon.ItemsSource = q
                    .OrderBy(x => x.SoCanHo)
                    .ThenBy(x => x.NgayLap)
                    .ToList();
            }
        }

        // Lấy ID từ dòng hiện tại
        private int? CurrentId()
        {
            if (dgHoaDon.SelectedItem == null) return null;

            // Ép sang object ẩn danh rồi lấy ID
            var row = dgHoaDon.SelectedItem;
            var prop = row.GetType().GetProperty("HoaDonID");
            return (int?)prop?.GetValue(row, null);
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadData(txtSearch.Text);

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn để xem."); return; }

            var win = new HoaDonCuDanDetailWindow(id.Value, readOnly: true);
            win.ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var win = new HoaDonCuDanDetailWindow(null);
            if (win.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn cần sửa."); return; }

            var win = new HoaDonCuDanDetailWindow(id.Value);
            if (win.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn cần xóa."); return; }

            if (MessageBox.Show($"Xóa hóa đơn {id}?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.HoaDonCuDans.FirstOrDefault(x => x.HoaDonID == id);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                try
                {
                    db.HoaDonCuDans.Remove(entity);
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
            txtSearch.Text = "";
            LoadData();
        }
    }
}
