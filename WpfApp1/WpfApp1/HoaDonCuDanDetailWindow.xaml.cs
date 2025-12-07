using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class HoaDonCuDanDetailWindow : Window
    {
        private readonly int? _id;
        private readonly bool _readOnly;
        private readonly QuanlychungcuEntities _context = new QuanlychungcuEntities();
        private HoaDonCuDan _hoaDon;

        public HoaDonCuDanDetailWindow(int? id = null, bool readOnly = false)
        {
            InitializeComponent();
            _id = id;
            _readOnly = readOnly;

            LoadCombos();
            LoadData();
            ApplyReadOnly();
        }

        // ==== Load dữ liệu cho combo ====
        private void LoadCombos()
        {
            cbCanHo.ItemsSource = _context.CanHoes
                                          .OrderBy(x => x.SoCanHo)
                                          .ToList();
            cbTrangThai.SelectedIndex = 0;     // ChuaThanhToan
            cbLoaiDichVu.SelectedIndex = 0;    // TienDien
            dpHanTT.SelectedDate = DateTime.Today;
        }

        // ==== Hiển thị khi mở ====
        private void LoadData()
        {
            if (_id == null)
            {
                _hoaDon = new HoaDonCuDan();   // thêm mới
                txtId.Text = "(tự tăng)";
                return;
            }

            _hoaDon = _context.HoaDonCuDans.Include(h => h.CuDan)
                                           .FirstOrDefault(h => h.HoaDonID == _id);
            if (_hoaDon == null)
            {
                MessageBox.Show("Không tìm thấy hóa đơn.", "Thông báo");
                DialogResult = false;
                Close();
                return;
            }

            txtId.Text = _hoaDon.HoaDonID.ToString();
            txtSoTien.Text = _hoaDon.SoTien.ToString("N0");
            dpHanTT.SelectedDate = _hoaDon.NgayLap;
            cbCanHo.SelectedValue = _hoaDon.CanHoID;
            txtNguoiQL.Text = _hoaDon.CuDan?.HoTen ?? "";

            // chọn trạng thái và loại dịch vụ theo text
            SelectComboByText(cbTrangThai, _hoaDon.TrangThai);
            SelectComboByText(cbLoaiDichVu, _hoaDon.LoaiDichVu);
        }

        private static void SelectComboByText(ComboBox cb, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            foreach (var it in cb.Items)
            {
                var s = (it as ComboBoxItem)?.Content?.ToString();
                if (string.Equals(s, text, StringComparison.OrdinalIgnoreCase))
                { cb.SelectedItem = it; break; }
            }
        }

        private void ApplyReadOnly()
        {
            if (!_readOnly) return;

            txtSoTien.IsReadOnly = true;
            dpHanTT.IsEnabled = false;
            cbTrangThai.IsEnabled = false;
            cbLoaiDichVu.IsEnabled = false;
            cbCanHo.IsEnabled = false;
            btnSave.Visibility = Visibility.Collapsed;
        }

        // Tự hiện người quản lý khi đổi căn hộ
        private void CbCanHo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCanHo.SelectedValue == null) { txtNguoiQL.Text = ""; return; }
            int canHoId = (int)cbCanHo.SelectedValue;
            var cuDan = _context.CuDans.FirstOrDefault(c => c.CanHoID == canHoId);
            txtNguoiQL.Text = cuDan?.HoTen ?? "(Chưa gán cư dân)";
        }

        // ==== Lưu ====
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1) Validate nhập liệu
                if (cbCanHo.SelectedValue == null)
                { MessageBox.Show("Vui lòng chọn Số căn hộ."); return; }

                if (!TryParseMoney(txtSoTien.Text, out decimal soTien))
                { MessageBox.Show("Số tiền không hợp lệ."); return; }

                string trangThai = (cbTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ChuaThanhToan";
                string loaiDv = (cbLoaiDichVu.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Khac";

                int canHoId = (int)cbCanHo.SelectedValue;
                var cuDan = _context.CuDans.FirstOrDefault(c => c.CanHoID == canHoId);
                if (cuDan == null)
                {
                    MessageBox.Show("Căn hộ này chưa gán cư dân nên không thể lập hóa đơn (bảng HoaDonCuDan yêu cầu CuDanID NOT NULL).");
                    return;
                }

                // 2) Gán vào entity
                _hoaDon.SoTien = soTien;
                _hoaDon.NgayLap = dpHanTT.SelectedDate ?? DateTime.Now; // cột DB tên NgayLap
                _hoaDon.TrangThai = trangThai;
                _hoaDon.LoaiDichVu = loaiDv;
                _hoaDon.CanHoID = canHoId;
                _hoaDon.CuDanID = cuDan.CuDanID;

                // 3) Thêm / sửa
                if (_hoaDon.HoaDonID == 0)
                    _context.HoaDonCuDans.Add(_hoaDon);
                else
                    _context.Entry(_hoaDon).State = EntityState.Modified;

                // 4) Save
                _context.SaveChanges();

                MessageBox.Show("Lưu hóa đơn thành công!", "Thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (DbEntityValidationException vex)
            {
                // In chi tiết lỗi validate của EF để dễ soi
                var sb = new StringBuilder();
                foreach (var e1 in vex.EntityValidationErrors)
                    foreach (var e2 in e1.ValidationErrors)
                        sb.AppendLine($"{e2.PropertyName}: {e2.ErrorMessage}");
                MessageBox.Show("Validation lỗi:\n" + sb, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu thất bại: " + ex.Message, "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool TryParseMoney(string input, out decimal value)
        {
            // chấp nhận “1.234.567”, “1,234,567”, “1234567”
            var raw = (input ?? "").Trim()
                                   .Replace(".", "")
                                   .Replace(",", "");
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
