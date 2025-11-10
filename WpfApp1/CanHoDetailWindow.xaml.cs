using System;
using System.Globalization;            // <-- thêm dòng này
using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class CanHoDetailWindow : Window
    {
        private readonly int? _id;       // null = thêm mới, có giá trị = sửa
        private readonly bool _readOnly; // true = chỉ xem

        public CanHoDetailWindow(int? canHoId, bool readOnly = false)
        {
            InitializeComponent();
            _id = canHoId;
            _readOnly = readOnly;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            using (var db = new QuanlychungcuEntities())
            {
                // Load danh sách tầng (SelectedValue = TangID, hiển thị TenTang)
                var tangs = db.Tangs
                              .Select(t => new { t.TangID, t.TenTang })
                              .OrderBy(t => t.TenTang)
                              .ToList();
                cbTang.ItemsSource = tangs;
                cbTang.DisplayMemberPath = "TenTang";
                cbTang.SelectedValuePath = "TangID";

                if (_id.HasValue)
                {
                    lblHeader.Text = "Thông tin căn hộ";
                    var ch = db.CanHoes.FirstOrDefault(x => x.CanHoID == _id.Value);
                    if (ch == null)
                    {
                        MessageBox.Show("Không tìm thấy căn hộ.");
                        Close();
                        return;
                    }

                    txtSoCanHo.Text = ch.SoCanHo;

                    // TangID là int (không nullable) => set thẳng, KHÔNG dùng .HasValue
                    cbTang.SelectedValue = ch.TangID;

                    // ToString theo Culture hiện tại
                    txtDienTich.Text = ch.DienTich?.ToString(CultureInfo.CurrentCulture);
                    txtGiaTri.Text = ch.GiaTri?.ToString(CultureInfo.CurrentCulture);

                    // Thống kê số cư dân (readonly)
                    txtSoCuDan.Text = db.CuDans.Count(c => c.CanHoID == ch.CanHoID).ToString();
                }
                else
                {
                    lblHeader.Text = "Thông tin căn hộ (Thêm)";
                    txtSoCuDan.Text = "0";
                }
            }

            if (_readOnly)
            {
                txtSoCanHo.IsReadOnly = true;
                cbTang.IsEnabled = false;
                txtDienTich.IsReadOnly = true;
                txtGiaTri.IsReadOnly = true;
                btnSave.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            var soRaw = txtSoCanHo.Text?.Trim();
            if (string.IsNullOrWhiteSpace(soRaw))
            { MessageBox.Show("Vui lòng nhập Số căn hộ."); return; }

            if (cbTang.SelectedValue == null)
            { MessageBox.Show("Vui lòng chọn Tầng."); return; }

            // Parse số theo Culture
            decimal? dienTich = null, giaTri = null;

            if (!string.IsNullOrWhiteSpace(txtDienTich.Text))
            {
                if (!decimal.TryParse(txtDienTich.Text.Trim(),
                                      NumberStyles.Number, CultureInfo.CurrentCulture, out var dt))
                { MessageBox.Show("Diện tích không hợp lệ."); return; }
                dienTich = dt;
            }

            if (!string.IsNullOrWhiteSpace(txtGiaTri.Text))
            {
                if (!decimal.TryParse(txtGiaTri.Text.Trim(),
                                      NumberStyles.Number, CultureInfo.CurrentCulture, out var gt))
                { MessageBox.Show("Giá trị không hợp lệ."); return; }
                giaTri = gt;
            }

            using (var db = new QuanlychungcuEntities())
            {
                try
                {
                    var tangId = (int)cbTang.SelectedValue;

                    if (_id == null)
                    {
                        // Chặn trùng SoCanHo (không phân biệt hoa thường + trim)
                        var norm = (soRaw ?? "").Trim().ToLowerInvariant();
                        bool existed = db.CanHoes
                            .AsEnumerable()
                            .Any(x => (x.SoCanHo ?? "").Trim().ToLowerInvariant() == norm);
                        if (existed) { MessageBox.Show("Số căn hộ đã tồn tại."); return; }

                        var entity = new CanHo
                        {
                            SoCanHo = soRaw,
                            TangID = tangId,
                            DienTich = dienTich,
                            GiaTri = giaTri
                        };
                        db.CanHoes.Add(entity);
                    }
                    else
                    {
                        var entity = db.CanHoes.FirstOrDefault(x => x.CanHoID == _id.Value);
                        if (entity == null) { MessageBox.Show("Không tìm thấy căn hộ để cập nhật."); return; }

                        var norm = (soRaw ?? "").Trim().ToLowerInvariant();
                        bool existed = db.CanHoes
                            .AsEnumerable()
                            .Any(x => x.CanHoID != entity.CanHoID &&
                                      (x.SoCanHo ?? "").Trim().ToLowerInvariant() == norm);
                        if (existed) { MessageBox.Show("Số căn hộ đã tồn tại."); return; }

                        entity.SoCanHo = soRaw;
                        entity.TangID = tangId;
                        entity.DienTich = dienTich;
                        entity.GiaTri = giaTri;
                    }

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể lưu: " + ex.Message, "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
