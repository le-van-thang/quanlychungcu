using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class XeMayDetailWindow : Window
    {
        private readonly int? _id;
        private readonly bool _readOnly;

        public XeMayDetailWindow(int? id, bool readOnly = false)
        {
            InitializeComponent();
            _id = id;
            _readOnly = readOnly;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object s, RoutedEventArgs e)
        {
            using (var db = new QuanlychungcuEntities())
            {
                cbCuDan.ItemsSource = db.CuDans.OrderBy(x => x.HoTen).ToList();

                if (_id.HasValue)
                {
                    lblHeader.Text = "Thông tin xe máy (Sửa)";
                    var entity = db.XeMays.FirstOrDefault(x => x.XeMayID == _id.Value);

                    if (entity == null)
                    {
                        MessageBox.Show("Không tìm thấy.");
                        DialogResult = false;
                        Close();
                        return;
                    }

                    txtId.Text = entity.XeMayID.ToString();
                    txtBKS.Text = entity.BKS;
                    cbCuDan.SelectedValue = entity.CuDanID;
                }
                else lblHeader.Text = "Thông tin xe máy (Thêm mới)";
            }

            if (_readOnly)
            {
                txtBKS.IsReadOnly = true;
                cbCuDan.IsEnabled = false;
                btnSave.IsEnabled = false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var bks = txtBKS.Text.Trim();
            if (string.IsNullOrWhiteSpace(bks)) { MessageBox.Show("Nhập biển số."); return; }
            if (cbCuDan.SelectedValue == null) { MessageBox.Show("Chọn cư dân."); return; }

            var cuDanId = (int)cbCuDan.SelectedValue;

            using (var db = new QuanlychungcuEntities())
            {
                bool trung = db.XeMays.Any(x => x.BKS == bks && (!_id.HasValue || x.XeMayID != _id.Value));
                if (trung) { MessageBox.Show("Biển số đã tồn tại."); return; }

                if (_id.HasValue)
                {
                    var entity = db.XeMays.First(x => x.XeMayID == _id.Value);
                    entity.BKS = bks;
                    entity.CuDanID = cuDanId;
                }
                else
                {
                    db.XeMays.Add(new XeMay { BKS = bks, CuDanID = cuDanId });
                }

                db.SaveChanges();
            }

            MessageBox.Show("Lưu thông tin xe máy thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
