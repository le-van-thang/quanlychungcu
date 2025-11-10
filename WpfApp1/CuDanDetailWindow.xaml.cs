using System;
using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class CuDanDetailWindow : Window
    {
        private string mode;
        private CuDan cuDan;

        public CuDanDetailWindow(string mode, CuDan cuDan = null)
        {
            InitializeComponent();
            this.mode = mode;
            this.cuDan = cuDan;

            LoadCanHo(); // load dữ liệu cho combobox căn hộ

            if (mode == "View" || mode == "Edit")
            {
                if (cuDan != null)
                {
                    txtId.Text = cuDan.CuDanID.ToString();
                    txtTen.Text = cuDan.HoTen;
                    dpNgaySinh.SelectedDate = cuDan.NgaySinh;
                    txtDienThoai.Text = cuDan.DienThoai;
                    txtEmail.Text = cuDan.Email;
                    cbCanHo.SelectedValue = cuDan.CanHoID;
                }

                if (mode == "View")
                {
                    txtId.IsReadOnly = true;
                    txtTen.IsReadOnly = true;
                    dpNgaySinh.IsEnabled = false;
                    txtDienThoai.IsReadOnly = true;
                    txtEmail.IsReadOnly = true;
                    cbCanHo.IsEnabled = false;
                    btnSave.Visibility = Visibility.Collapsed;
                }
            }
            else if (mode == "Add")
            {
                txtId.IsEnabled = false; // ID sẽ tự sinh trong DB
                txtId.Text = "(Tự sinh)";
            }
        }

        private void LoadCanHo()
        {
            using (var db = new QuanlychungcuEntities())
            {
                cbCanHo.ItemsSource = db.CanHoes
                    .Select(x => new { x.CanHoID, x.SoCanHo })
                    .OrderBy(x => x.SoCanHo)
                    .ToList();

                cbCanHo.DisplayMemberPath = "SoCanHo";
                cbCanHo.SelectedValuePath = "CanHoID";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTen.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên!");
                return;
            }

            using (var db = new QuanlychungcuEntities())
            {
                if (mode == "Add")
                {
                    var entity = new CuDan
                    {
                        HoTen = txtTen.Text.Trim(),
                        NgaySinh = dpNgaySinh.SelectedDate,
                        DienThoai = txtDienThoai.Text?.Trim(),
                        Email = txtEmail.Text?.Trim(),
                        CanHoID = cbCanHo.SelectedValue as int?
                    };
                    db.CuDans.Add(entity);
                }
                else if (mode == "Edit" && cuDan != null)
                {
                    var entity = db.CuDans.FirstOrDefault(c => c.CuDanID == cuDan.CuDanID);
                    if (entity != null)
                    {
                        entity.HoTen = txtTen.Text.Trim();
                        entity.NgaySinh = dpNgaySinh.SelectedDate;
                        entity.DienThoai = txtDienThoai.Text?.Trim();
                        entity.Email = txtEmail.Text?.Trim();
                        entity.CanHoID = cbCanHo.SelectedValue as int?;
                    }
                }

                try
                {
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
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
