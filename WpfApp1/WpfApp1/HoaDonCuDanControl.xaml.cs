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
            if (_currentUser == null) return;

            bool canEdit = RoleHelper.CanEditData(_currentUser);

            btnAdd.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnDelete.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool CanEdit => RoleHelper.CanEditData(_currentUser);

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
                        LoaiDichVu = h.LoaiDichVu,
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
                        (x.TrangThai ?? "").ToLower().Contains(keyword));
                }

                dgHoaDon.ItemsSource = q
                    .OrderBy(x => x.SoCanHo)
                    .ThenBy(x => x.NgayLap)
                    .ToList();
            }
        }

        private int? CurrentId()
        {
            if (dgHoaDon.SelectedItem == null) return null;
            var row = dgHoaDon.SelectedItem;
            var prop = row.GetType().GetProperty("HoaDonID");
            return (int?)prop?.GetValue(row, null);
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var kw = txtSearch.Text;
            LoadData(kw);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                AuditLogger.Log("Search", "HoaDonCuDan", null,
                    $"Tìm kiếm hóa đơn cư dân với từ khóa: \"{kw}\"");
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn để xem."); return; }

            new HoaDonCuDanDetailWindow(id.Value, readOnly: true).ShowDialog();

            using (var db = new QuanlychungcuEntities())
            {
                var h = db.HoaDonCuDans.FirstOrDefault(x => x.HoaDonID == id.Value);
                if (h != null)
                {
                    AuditLogger.Log("View", "HoaDonCuDan", id.Value.ToString(),
                        $"Xem hóa đơn cư dân: CanHo={h.CanHo?.SoCanHo}, CuDan={h.CuDan?.HoTen}, LoaiDV={h.LoaiDichVu}");
                }
                else
                {
                    AuditLogger.Log("View", "HoaDonCuDan", id.Value.ToString(),
                        "Xem hóa đơn cư dân (không tìm thấy chi tiết trong DB)");
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được thêm hóa đơn.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new HoaDonCuDanDetailWindow(null);
            if (win.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Create", "HoaDonCuDan", null,
                    "Thêm hóa đơn cư dân mới");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được sửa hóa đơn.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn cần sửa."); return; }

            var win = new HoaDonCuDanDetailWindow(id.Value);
            if (win.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Update", "HoaDonCuDan", id.Value.ToString(),
                    "Sửa hóa đơn cư dân (ID=" + id.Value + ")");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được xóa hóa đơn.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn cần xóa."); return; }

            if (MessageBox.Show($"Xóa hóa đơn {id}?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.HoaDonCuDans.FirstOrDefault(x => x.HoaDonID == id);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                db.HoaDonCuDans.Remove(entity);
                db.SaveChanges();
            }

            AuditLogger.Log("Delete", "HoaDonCuDan", id.Value.ToString(),
                "Xóa hóa đơn cư dân (ID=" + id.Value + ")");

            LoadData(txtSearch.Text);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();
        }
    }
}
