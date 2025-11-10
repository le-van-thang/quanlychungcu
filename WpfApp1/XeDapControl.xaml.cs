using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class XeDapControl : UserControl
    {
        private TaiKhoan _currentUser;

        // Ctor mặc định (designer hoặc không cần phân quyền)
        public XeDapControl()
        {
            InitializeComponent();
            ApplyRolePermission();  // nếu chưa có _currentUser thì coi như không phải admin
            LoadData();
        }

        // Ctor có user (gọi từ HomeControl)
        public XeDapControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            ApplyRolePermission();
        }

        // Ẩn/hiện các nút theo vai trò
        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            if (FindName("btnAdd") is Button add) add.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnEdit") is Button edit) edit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnDelete") is Button del) del.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnThem") is Button them) them.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnSua") is Button sua) sua.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnXoa") is Button xoa) xoa.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadData(string keyword = null)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = db.XeDaps.Select(x => new XeDapRow
                {
                    XeDapID = x.XeDapID,
                    BKS = x.BKS,
                    CuDanID = x.CuDanID,
                    TenCuDan = x.CuDan != null ? x.CuDan.HoTen : null
                });

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var k = keyword.Trim().ToLower();
                    q = q.Where(x =>
                        (x.BKS ?? "").ToLower().Contains(k) ||
                        (x.TenCuDan ?? "").ToLower().Contains(k));
                }

                if (FindName("dgXeDap") is DataGrid grid)
                {
                    grid.ItemsSource = q
                        .OrderBy(x => x.TenCuDan)
                        .ThenBy(x => x.BKS)
                        .ToList();
                }
            }
        }

        private XeDapRow Current()
            => (FindName("dgXeDap") as DataGrid)?.SelectedItem as XeDapRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => LoadData((FindName("txtSearch") as TextBox)?.Text);

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("txtSearch") is TextBox t) t.Text = "";
            LoadData();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe đạp."); return; }
            new XeDapDetailWindow(row.XeDapID, readOnly: true).ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var w = new XeDapDetailWindow(null);
            if (w.ShowDialog() == true)
                LoadData((FindName("txtSearch") as TextBox)?.Text);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe cần sửa."); return; }

            var w = new XeDapDetailWindow(row.XeDapID);
            if (w.ShowDialog() == true)
                LoadData((FindName("txtSearch") as TextBox)?.Text);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe cần xóa."); return; }

            if (MessageBox.Show($"Xóa xe đạp biển số [{row.BKS}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.XeDaps.FirstOrDefault(x => x.XeDapID == row.XeDapID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                try
                {
                    db.XeDaps.Remove(entity);
                    db.SaveChanges();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Không xóa được: " + ex.Message);
                    return;
                }
            }

            LoadData((FindName("txtSearch") as TextBox)?.Text);
        }
    }

    public class XeDapRow
    {
        public int XeDapID { get; set; }
        public string BKS { get; set; }
        public int CuDanID { get; set; }
        public string TenCuDan { get; set; }
    }
}
