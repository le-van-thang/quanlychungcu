using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class MatBangThuongMaiDetailWindow : Window
    {
        private readonly int? _id;       // null = thêm mới
        private readonly bool _readOnly; // true = chỉ xem

        public MatBangThuongMaiDetailWindow()
            : this(null, false)
        {
        }

        public MatBangThuongMaiDetailWindow(int? id, bool readOnly = false)
        {
            InitializeComponent();
            _id = id;
            _readOnly = readOnly;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            using (var db = new QuanlychungcuEntities())
            {
                if (_id.HasValue)
                {
                    lblHeader.Text = "Thông tin mặt bằng";
                    var mb = db.MatBangThuongMais
                               .FirstOrDefault(x => x.MatBangID == _id.Value);
                    if (mb == null)
                    {
                        MessageBox.Show("Không tìm thấy mặt bằng.");
                        Close();
                        return;
                    }

                    txtId.Text = mb.MatBangID.ToString();
                    txtTenMatBang.Text = mb.TenMatBang;
                    txtDienTich.Text = mb.DienTich?.ToString(CultureInfo.InvariantCulture);
                    txtGiaThue.Text = mb.GiaThue?.ToString(CultureInfo.InvariantCulture);
                    txtGhiChu.Text = mb.GhiChu;
                }
                else
                {
                    lblHeader.Text = "Thông tin mặt bằng (Thêm)";
                    txtId.Text = "(tự tăng)";
                }
            }

            if (_readOnly)
            {
                txtTenMatBang.IsReadOnly = true;
                txtDienTich.IsReadOnly = true;
                txtGiaThue.IsReadOnly = true;
                txtGhiChu.IsReadOnly = true;
                btnSave.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenMatBang.Text))
            {
                MessageBox.Show("Vui lòng nhập Tên mặt bằng.");
                return;
            }

            if (!decimal.TryParse(txtDienTich.Text,
                                  NumberStyles.Any,
                                  CultureInfo.InvariantCulture,
                                  out var dienTich))
            {
                MessageBox.Show("Diện tích không hợp lệ.");
                return;
            }

            if (!decimal.TryParse(txtGiaThue.Text,
                                  NumberStyles.Any,
                                  CultureInfo.InvariantCulture,
                                  out var giaThue))
            {
                MessageBox.Show("Giá thuê không hợp lệ.");
                return;
            }

            using (var db = new QuanlychungcuEntities())
            {
                if (_id == null)
                {
                    var entity = new MatBangThuongMai
                    {
                        TenMatBang = txtTenMatBang.Text.Trim(),
                        DienTich = dienTich,
                        GiaThue = giaThue,
                        GhiChu = txtGhiChu.Text.Trim()
                    };
                    db.MatBangThuongMais.Add(entity);
                }
                else
                {
                    var entity = db.MatBangThuongMais
                                   .First(x => x.MatBangID == _id.Value);
                    entity.TenMatBang = txtTenMatBang.Text.Trim();
                    entity.DienTich = dienTich;
                    entity.GiaThue = giaThue;
                    entity.GhiChu = txtGhiChu.Text.Trim();
                }

                db.SaveChanges();
            }

            DialogResult = true;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
