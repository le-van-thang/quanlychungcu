using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class MatBangThuongMaiControl : UserControl
    {
        public MatBangThuongMaiControl()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData(string keyword = null)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = db.MatBangThuongMais
                          .Select(m => new MatBangRow
                          {
                              MatBangID = m.MatBangID,
                              TenMatBang = m.TenMatBang,
                              DienTich = m.DienTich,
                              GiaThue = m.GiaThue
                          });

                // Nếu có từ khóa tìm kiếm thì lọc theo tên mặt bằng
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower();
                    q = q.Where(x => (x.TenMatBang ?? "").ToLower().Contains(keyword));
                }

                dgMBTM.ItemsSource = q
                    .OrderBy(x => x.MatBangID)
                    .ToList();
            }
        }

        private MatBangRow Current() => dgMBTM.SelectedItem as MatBangRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadData(txtSearch.Text);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();     // Xóa ô tìm kiếm
            LoadData();            // Nạp lại toàn bộ dữ liệu
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 mặt bằng."); return; }
            var w = new MatBangThuongMaiDetailWindow(row.MatBangID, readOnly: true);
            w.ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var w = new MatBangThuongMaiDetailWindow(null);
            if (w.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 mặt bằng cần sửa."); return; }
            var w = new MatBangThuongMaiDetailWindow(row.MatBangID);
            if (w.ShowDialog() == true) LoadData(txtSearch.Text);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 mặt bằng cần xóa."); return; }
            if (MessageBox.Show($"Xóa mặt bằng [{row.TenMatBang}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.MatBangThuongMais.FirstOrDefault(x => x.MatBangID == row.MatBangID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                try
                {
                    db.MatBangThuongMais.Remove(entity);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không xóa được: " + ex.Message);
                    return;
                }
            }

            LoadData(txtSearch.Text);
        }
    }

    public class MatBangRow
    {
        public int MatBangID { get; set; }
        public string TenMatBang { get; set; }
        public decimal? DienTich { get; set; }
        public decimal? GiaThue { get; set; }
    }
}
