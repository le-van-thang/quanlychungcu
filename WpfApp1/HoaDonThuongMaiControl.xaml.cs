using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class HoaDonThuongMaiControl : UserControl
    {
        private TaiKhoan _currentUser;

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
            ApplyRolePermission();
        }

        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            if (FindName("btnAdd") is Button add) add.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnEdit") is Button edt) edt.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnDelete") is Button del) del.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadData(string keyword = null)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = db.HoaDonTMs.Select(h => new
                {
                    h.HoaDonTMID,
                    TenMB = h.MatBangThuongMai.TenMatBang,
                    h.NoiDung,
                    h.SoTien,              // luôn là TỔNG tiền
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

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadData(txtSearch.Text);

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn TM để xem."); return; }
            new HoaDonTMDetailWindow(id.Value, readOnly: true).ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var win = new HoaDonTMDetailWindow(null);
            if (win.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn TM cần sửa."); return; }
            var win = new HoaDonTMDetailWindow(id.Value);
            if (win.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var id = CurrentId();
            if (id == null) { MessageBox.Show("Chọn 1 hóa đơn TM cần xóa."); return; }

            if (MessageBox.Show($"Xóa hóa đơn TM {id}?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (var db = new QuanlychungcuEntities())
                using (var tx = db.Database.BeginTransaction())
                {
                    var hd = db.HoaDonTMs.FirstOrDefault(x => x.HoaDonTMID == id);
                    if (hd == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                    // 1) Lấy & xóa chi tiết, đồng thời trả tồn kho
                    var details = db.HoaDonTM_ChiTiet.Where(ct => ct.HoaDonTMID == hd.HoaDonTMID).ToList();
                    foreach (var ct in details)
                    {
                        var vt = db.VatTus.FirstOrDefault(v => v.VatTuID == ct.VatTuID);
                        if (vt != null) vt.SoLuong = (vt.SoLuong ?? 0) + ct.SoLuong;

                        db.HoaDonTM_ChiTiet.Remove(ct);
                    }

                    // 2) Xóa hóa đơn
                    db.HoaDonTMs.Remove(hd);

                    db.SaveChanges();
                    tx.Commit();
                }

                LoadData(txtSearch.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể xóa: " + ex.GetBaseException().Message, "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();
        }
    }
}
