using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class HoaDonThuongMaiControl : UserControl
    {
        private readonly TaiKhoan _currentUser;

        public HoaDonThuongMaiControl(TaiKhoan user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadData();
            ApplyRolePermission();
        }

        public HoaDonThuongMaiControl()
        {
            InitializeComponent();
            LoadData();
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
                var q = db.HoaDonTMs.Select(h => new
                {
                    h.HoaDonTMID,
                    TenMB = h.MatBangThuongMai.TenMatBang,
                    h.NoiDung,
                    h.SoTien,
                    h.NgayLap,
                    h.TrangThai
                });

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower();
                    q = q.Where(x =>
                        (x.TenMB ?? "").ToLower().Contains(keyword) ||
                        (x.NoiDung ?? "").ToLower().Contains(keyword) ||
                        (x.TrangThai ?? "").ToLower().Contains(keyword) ||
                        x.SoTien.ToString().Contains(keyword));
                }

                dgHoaDonTM.ItemsSource = q
                    .OrderByDescending(x => x.NgayLap)
                    .ThenBy(x => x.TenMB)
                    .ToList();
            }
        }

        private int? CurrentId()
        {
            if (dgHoaDonTM.SelectedItem == null) return null;
            var row = dgHoaDonTM.SelectedItem;
            var prop = row.GetType().GetProperty("HoaDonTMID");
            return (int?)prop?.GetValue(row, null);
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => DoSearch();

        private void DoSearch()
        {
            var kw = txtSearch.Text;
            LoadData(kw);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                AuditLogger.Log("Search", "HoaDonThuongMai", null,
                    $"Tìm kiếm hóa đơn TM với từ khóa: \"{kw}\"");
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null)
            {
                MessageBox.Show("Chọn 1 hóa đơn TM để xem.");
                return;
            }

            new HoaDonTMDetailWindow(id.Value, readOnly: true).ShowDialog();

            using (var db = new QuanlychungcuEntities())
            {
                var hd = db.HoaDonTMs.FirstOrDefault(x => x.HoaDonTMID == id.Value);
                if (hd != null)
                {
                    AuditLogger.Log("View", "HoaDonThuongMai", id.Value.ToString(),
                        $"Xem hóa đơn TM: MatBang={hd.MatBangThuongMai?.TenMatBang}, NoiDung={hd.NoiDung}");
                }
                else
                {
                    AuditLogger.Log("View", "HoaDonThuongMai", id.Value.ToString(),
                        "Xem hóa đơn TM (không tìm thấy trong DB)");
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được thêm hóa đơn TM.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new HoaDonTMDetailWindow(null);
            if (win.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Create", "HoaDonThuongMai", null,
                    "Thêm hóa đơn thương mại mới");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được sửa hóa đơn TM.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var id = CurrentId();
            if (id == null)
            {
                MessageBox.Show("Chọn 1 hóa đơn TM cần sửa.");
                return;
            }

            var win = new HoaDonTMDetailWindow(id.Value);
            if (win.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Update", "HoaDonThuongMai", id.Value.ToString(),
                    "Sửa hóa đơn thương mại (ID=" + id.Value + ")");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được xóa hóa đơn TM.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var id = CurrentId();
            if (id == null)
            {
                MessageBox.Show("Chọn 1 hóa đơn TM cần xóa.");
                return;
            }

            if (MessageBox.Show($"Xóa hóa đơn TM {id}?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using (var db = new QuanlychungcuEntities())
            {
                var hd = db.HoaDonTMs.FirstOrDefault(x => x.HoaDonTMID == id);
                if (hd == null)
                {
                    MessageBox.Show("Không tìm thấy bản ghi.");
                    return;
                }

                var details = db.HoaDonTM_ChiTiet
                    .Where(ct => ct.HoaDonTMID == hd.HoaDonTMID)
                    .ToList();

                foreach (var ct in details)
                {
                    var vt = db.VatTus.FirstOrDefault(v => v.VatTuID == ct.VatTuID);
                    if (vt != null)
                        vt.SoLuong = (vt.SoLuong ?? 0) + ct.SoLuong;

                    db.HoaDonTM_ChiTiet.Remove(ct);
                }

                db.HoaDonTMs.Remove(hd);
                db.SaveChanges();
            }

            AuditLogger.Log("Delete", "HoaDonThuongMai", id.Value.ToString(),
                "Xóa hóa đơn thương mại (ID=" + id.Value + ") và hoàn trả tồn kho vật tư");

            LoadData(txtSearch.Text);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();
        }
    }
}
