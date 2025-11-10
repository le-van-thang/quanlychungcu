using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class XeOToControl : UserControl
    {
        private TaiKhoan _currentUser;

        // ctor mặc định (designer, hoặc không cần phân quyền)
        public XeOToControl()
        {
            InitializeComponent();
            ApplyRolePermission();   // sẽ ẩn/hiện nếu có _currentUser
            LoadData();
        }

        // ctor có user (khuyến nghị dùng từ HomeControl)
        public XeOToControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            ApplyRolePermission();
        }

        // Ẩn/hiện các nút theo vai trò (Admin/QuanLy mới thấy)
        private void ApplyRolePermission()
        {
            var role = _currentUser?.VaiTro?.ToLower() ?? "";
            bool isAdmin = role == "admin" || role == "administrator" || role == "quanly";

            // Hỗ trợ cả 2 kiểu tên nút: btnAdd/btnEdit/btnDelete hoặc btnThem/btnSua/btnXoa
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
                var q = db.XeOToes.Select(x => new XeOToRow
                {
                    XeOToID = x.XeOToID,
                    BKS = x.BKS,
                    CuDanID = x.CuDanID,
                    TenCuDan = x.CuDan != null ? x.CuDan.HoTen : null
                });

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var k = keyword.Trim().ToLower();
                    q = q.Where(x =>
                        (!string.IsNullOrEmpty(x.BKS) && x.BKS.ToLower().Contains(k)) ||
                        (!string.IsNullOrEmpty(x.TenCuDan) && x.TenCuDan.ToLower().Contains(k))
                    );
                }

                if (FindName("dgXeOTo") is DataGrid grid)
                {
                    grid.ItemsSource = q
                        .OrderBy(x => x.TenCuDan)
                        .ThenBy(x => x.BKS)
                        .ToList();
                }
            }
        }

        private XeOToRow Current()
            => (FindName("dgXeOTo") as DataGrid)?.SelectedItem as XeOToRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string kw = (FindName("txtSearch") as TextBox)?.Text;
            LoadData(kw);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("txtSearch") is TextBox t) t.Text = string.Empty;
            LoadData();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 ô tô."); return; }
            new XeOToDetailWindow(row.XeOToID, readOnly: true).ShowDialog();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var w = new XeOToDetailWindow(null);
            if (w.ShowDialog() == true)
            {
                string kw = (FindName("txtSearch") as TextBox)?.Text;
                LoadData(kw);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 ô tô cần sửa."); return; }

            var w = new XeOToDetailWindow(row.XeOToID);
            if (w.ShowDialog() == true)
            {
                string kw = (FindName("txtSearch") as TextBox)?.Text;
                LoadData(kw);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 ô tô cần xóa."); return; }

            if (MessageBox.Show($"Xóa ô tô biển số [{row.BKS}]?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.XeOToes.FirstOrDefault(x => x.XeOToID == row.XeOToID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                try
                {
                    db.XeOToes.Remove(entity);
                    db.SaveChanges();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Không xóa được: " + ex.Message);
                    return;
                }
            }

            string kw2 = (FindName("txtSearch") as TextBox)?.Text;
            LoadData(kw2);
        }
    }

    public class XeOToRow
    {
        public int XeOToID { get; set; }
        public string BKS { get; set; }
        public int CuDanID { get; set; }
        public string TenCuDan { get; set; }
    }
}
