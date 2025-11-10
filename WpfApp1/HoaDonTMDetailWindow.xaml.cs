using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;

namespace WpfApp1
{
    public partial class HoaDonTMDetailWindow : Window
    {
        private readonly int? _id;       // null = thêm mới
        private readonly bool _readOnly; // chỉ xem

        public HoaDonTMDetailWindow(int? id, bool readOnly = false)
        {
            InitializeComponent();
            _id = id;
            _readOnly = readOnly;

            LoadMatBang();

            if (_id != null) LoadData();
            else
            {
                dpHanTT.SelectedDate = DateTime.Today;
                cbTrangThai.SelectedIndex = 0;
                txtSoTien.Text = "0";
            }

            ApplyReadOnly();
        }

        // ========= Helpers =========

        private static decimal ParseMoney(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0m;
            input = input.Replace(".", "").Replace(",", "").Trim();
            return decimal.TryParse(input, out var v) ? v : 0m;
        }

        private static decimal GetDetailTotal(QuanlychungcuEntities db, int hoaDonId)
        {
            return db.HoaDonTM_ChiTiet
                     .Where(ct => ct.HoaDonTMID == hoaDonId)
                     .Select(ct => (decimal?)(ct.SoLuong * ct.DonGia))
                     .Sum() ?? 0m;
        }

        // ================== LOAD UI ==================

        private void LoadMatBang()
        {
            using (var db = new QuanlychungcuEntities())
            {
                cbMatBang.ItemsSource = db.MatBangThuongMais
                    .Select(mb => new { mb.MatBangID, mb.TenMatBang })
                    .OrderBy(x => x.TenMatBang)
                    .ToList();

                cbMatBang.DisplayMemberPath = "TenMatBang";
                cbMatBang.SelectedValuePath = "MatBangID";
            }
        }

        private void LoadData()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var hd = db.HoaDonTMs.FirstOrDefault(x => x.HoaDonTMID == _id);
                if (hd == null) return;

                // Hiển thị TỔNG TIỀN (không phải base)
                txtId.Text = hd.HoaDonTMID.ToString();
                txtSoTien.Text = hd.SoTien.ToString("N0");
                dpHanTT.SelectedDate = hd.NgayLap;
                cbTrangThai.Text = hd.TrangThai;
                cbMatBang.SelectedValue = hd.MatBangID;
                txtGhiChu.Text = hd.NoiDung;

                dgChiTiet.ItemsSource = db.HoaDonTM_ChiTiet
                    .Where(ct => ct.HoaDonTMID == hd.HoaDonTMID)
                    .Select(ct => new
                    {
                        TenVatTu = ct.VatTu.TenVatTu,
                        ct.SoLuong,
                        ct.DonGia,
                        ThanhTien = (decimal)(ct.SoLuong * ct.DonGia)
                    })
                    .ToList();
            }
        }

        private void ApplyReadOnly()
        {
            if (!_readOnly) return;
            txtSoTien.IsReadOnly = true;
            dpHanTT.IsEnabled = false;
            cbTrangThai.IsEnabled = false;
            cbMatBang.IsEnabled = false;
            txtGhiChu.IsReadOnly = true;
            btnSave.Visibility = Visibility.Collapsed;
        }

        // ================== LƯU ==================

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new QuanlychungcuEntities())
                {
                    if (cbMatBang.SelectedValue == null)
                    {
                        MessageBox.Show("Vui lòng chọn Mặt bằng thương mại.");
                        return;
                    }

                    HoaDonTM entity;

                    if (_id == null)
                    {
                        entity = new HoaDonTM();
                        db.HoaDonTMs.Add(entity);
                    }
                    else
                    {
                        entity = db.HoaDonTMs.FirstOrDefault(x => x.HoaDonTMID == _id);
                        if (entity == null)
                        {
                            MessageBox.Show("Không tìm thấy hóa đơn!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // tổng tiền người dùng nhập trong textbox
                    var inputTotal = ParseMoney(txtSoTien.Text);

                    // Nếu đang sửa hóa đơn cũ, cảnh báo khi tổng < tổng chi tiết hiện có
                    var currentDetail = entity.HoaDonTMID == 0 ? 0m : GetDetailTotal(db, entity.HoaDonTMID);
                    if (inputTotal < currentDetail)
                    {
                        var msg = $"Tổng tiền nhập ({inputTotal:N0}) nhỏ hơn tổng chi tiết hiện có ({currentDetail:N0}).\n" +
                                  $"Hệ thống sẽ dùng tổng chi tiết.";
                        MessageBox.Show(msg, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        inputTotal = currentDetail;
                    }

                    // thông tin chung
                    entity.MatBangID = (int)cbMatBang.SelectedValue;
                    entity.TrangThai = (cbTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chưa thanh toán";
                    entity.NgayLap = dpHanTT.SelectedDate ?? DateTime.Now;
                    entity.NoiDung = txtGhiChu.Text?.Trim();

                    // LƯU TỔNG TIỀN
                    entity.SoTien = inputTotal;

                    db.SaveChanges();

                    MessageBox.Show("💾 Lưu thành công!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi lưu: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============== THÊM VẬT TƯ ==============

        private void BtnThemVatTu_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ChonVatTuWindow();
            if (dlg.ShowDialog() != true) return;

            if (_id == null)
            {
                MessageBox.Show("Hãy lưu hóa đơn trước khi thêm vật tư!",
                    "Nhắc nhở", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new QuanlychungcuEntities())
            {
                var hoaDon = db.HoaDonTMs.FirstOrDefault(x => x.HoaDonTMID == _id);
                if (hoaDon == null) return;

                // Tổng chi tiết TRƯỚC khi thêm
                var oldDetail = GetDetailTotal(db, hoaDon.HoaDonTMID);

                // Thêm vào chi tiết + trừ tồn
                foreach (var vt in dlg.SelectedVatTus)
                {
                    var vatTuDb = db.VatTus.FirstOrDefault(x => x.VatTuID == vt.VatTuID);
                    if (vatTuDb == null) continue;

                    var ton = vatTuDb.SoLuong ?? 0;
                    if (vt.SoLuongChon <= 0) continue;

                    if (vt.SoLuongChon > ton)
                    {
                        MessageBox.Show(
                            $"Vật tư '{vatTuDb.TenVatTu}' chỉ còn {ton}. Không thể thêm {vt.SoLuongChon}.",
                            "Thiếu tồn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    var ct = db.HoaDonTM_ChiTiet
                        .FirstOrDefault(x => x.HoaDonTMID == hoaDon.HoaDonTMID && x.VatTuID == vt.VatTuID);

                    if (ct == null)
                    {
                        ct = new HoaDonTM_ChiTiet
                        {
                            HoaDonTMID = hoaDon.HoaDonTMID,
                            VatTuID = vt.VatTuID,
                            SoLuong = vt.SoLuongChon,
                            DonGia = vt.Gia
                        };
                        db.HoaDonTM_ChiTiet.Add(ct);
                    }
                    else
                    {
                        ct.SoLuong += vt.SoLuongChon;
                    }

                    // trừ tồn kho
                    vatTuDb.SoLuong = ton - vt.SoLuongChon;
                }

                // Lưu chi tiết trước, để tổng chi tiết mới phản ánh đúng ở DB
                db.SaveChanges();

                // Tính lại tổng: base = max(tổng cũ - oldDetail, 0)
                var newDetail = GetDetailTotal(db, hoaDon.HoaDonTMID);
                var baseAmount = Math.Max(hoaDon.SoTien - oldDetail, 0m);
                hoaDon.SoTien = baseAmount + newDetail;

                db.SaveChanges();
            }

            LoadData(); // refresh chi tiết + ô tổng
        }

        // ============== XÓA/TRẢ BỚT VẬT TƯ ==============

        private void BtnXoaVatTu_Click(object sender, RoutedEventArgs e)
        {
            if (_id == null) return;

            var row = (sender as FrameworkElement)?.DataContext;
            var tenVatTu = row?.GetType().GetProperty("TenVatTu")?.GetValue(row)?.ToString();
            if (string.IsNullOrWhiteSpace(tenVatTu)) return;

            using (var db = new QuanlychungcuEntities())
            {
                var hoaDon = db.HoaDonTMs.FirstOrDefault(x => x.HoaDonTMID == _id);
                if (hoaDon == null) return;

                var ct = db.HoaDonTM_ChiTiet
                    .FirstOrDefault(x => x.HoaDonTMID == hoaDon.HoaDonTMID && x.VatTu.TenVatTu == tenVatTu);
                if (ct == null) return;

                var input = Interaction.InputBox(
                    $"Nhập số lượng muốn trả (tối đa {ct.SoLuong}):", "Trả vật tư", "1");
                if (!int.TryParse(input, out var soLuongTra) || soLuongTra <= 0 || soLuongTra > ct.SoLuong)
                {
                    MessageBox.Show("Số lượng không hợp lệ!"); return;
                }

                // Tổng chi tiết trước khi chỉnh
                var oldDetail = GetDetailTotal(db, hoaDon.HoaDonTMID);

                // trả tồn kho
                var vatTuDb = db.VatTus.FirstOrDefault(v => v.VatTuID == ct.VatTuID);
                if (vatTuDb != null)
                    vatTuDb.SoLuong = (vatTuDb.SoLuong ?? 0) + soLuongTra;

                // giảm/xóa chi tiết
                if (soLuongTra == ct.SoLuong) db.HoaDonTM_ChiTiet.Remove(ct);
                else ct.SoLuong -= soLuongTra;

                // Lưu thay đổi chi tiết trước
                db.SaveChanges();

                // Tính lại tổng
                var newDetail = GetDetailTotal(db, hoaDon.HoaDonTMID);
                var baseAmount = Math.Max(hoaDon.SoTien - oldDetail, 0m);
                hoaDon.SoTien = baseAmount + newDetail;

                db.SaveChanges();
            }

            LoadData();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
