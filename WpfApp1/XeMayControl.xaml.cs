using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class XeMayControl : UserControl
    {
        private TaiKhoan _currentUser;

        // Constructor mặc định
        public XeMayControl()
        {
            InitializeComponent();
            ApplyRolePermission();
            LoadData();
        }

        // Constructor có user (khuyến nghị dùng từ HomeControl)
        public XeMayControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            ApplyRolePermission();
        }

        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            if (FindName("btnAdd") is Button add) add.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnEdit") is Button edit) edit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnDelete") is Button del) del.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // nếu nút trong XAML của bạn đặt tên btnThem/btnSua/btnXoa thì cũng hỗ trợ
            if (FindName("btnThem") is Button them) them.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnSua") is Button sua) sua.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnXoa") is Button xoa) xoa.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadData(string keyword = null)
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = db.XeMays.Select(x => new XeMayRow
                {
                    XeMayID = x.XeMayID,
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

                if (FindName("dgXeMay") is DataGrid grid)
                {
                    grid.ItemsSource = q
                        .OrderBy(x => x.TenCuDan)
                        .ThenBy(x => x.BKS)
                        .ToList();
                }
            }
        }

        private XeMayRow Current() => (FindName("dgXeMay") as DataGrid)?.SelectedItem as XeMayRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => LoadData((FindName("txtSearch") as TextBox)?.Text);

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("txtSearch") is TextBox t) t.Clear();
            LoadData();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe máy."); return; }
            new XeMayDetailWindow(row.XeMayID, readOnly: true).ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var w = new XeMayDetailWindow(null);
            if (w.ShowDialog() == true)
                LoadData((FindName("txtSearch") as TextBox)?.Text);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe máy cần sửa."); return; }

            var w = new XeMayDetailWindow(row.XeMayID);
            if (w.ShowDialog() == true)
                LoadData((FindName("txtSearch") as TextBox)?.Text);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe máy cần xóa."); return; }

            if (MessageBox.Show($"Xóa xe máy biển số [{row.BKS}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.XeMays.FirstOrDefault(x => x.XeMayID == row.XeMayID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                try
                {
                    db.XeMays.Remove(entity);
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

    public class XeMayRow
    {
        public int XeMayID { get; set; }
        public string BKS { get; set; }
        public int CuDanID { get; set; }
        public string TenCuDan { get; set; }
    }
}
