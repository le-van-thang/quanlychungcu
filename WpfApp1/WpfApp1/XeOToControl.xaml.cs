using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class XeOToControl : UserControl
    {
        private TaiKhoan _currentUser;

        public XeOToControl()
        {
            InitializeComponent();
            LoadData();
        }

        public XeOToControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            ApplyRolePermission();
        }

        private void ApplyRolePermission()
        {
            if (_currentUser == null) return;

            bool canEdit = RoleHelper.CanEditData(_currentUser);

            if (FindName("btnAdd") is Button add) add.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnEdit") is Button edit) edit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnDelete") is Button del) del.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

            if (FindName("btnThem") is Button them) them.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnSua") is Button sua) sua.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("btnXoa") is Button xoa) xoa.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool CanEdit => RoleHelper.CanEditData(_currentUser);

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
                        (x.BKS ?? "").ToLower().Contains(k) ||
                        (x.TenCuDan ?? "").ToLower().Contains(k));
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
            var txt = FindName("txtSearch") as TextBox;
            var kw = txt?.Text;
            LoadData(kw);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                AuditLogger.Log("Search", "XeOTo", null,
                    $"Tìm kiếm ô tô với từ khóa: \"{kw}\"");
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("txtSearch") is TextBox t) t.Text = "";
            LoadData();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 ô tô."); return; }
            new XeOToDetailWindow(row.XeOToID, readOnly: true).ShowDialog();

            AuditLogger.Log("View", "XeOTo", row.XeOToID.ToString(),
                $"Xem ô tô BKS={row.BKS}, Cư dân={row.TenCuDan}");
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được thêm ô tô.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var w = new XeOToDetailWindow(null);
            if (w.ShowDialog() == true)
            {
                LoadData((FindName("txtSearch") as TextBox)?.Text);
                AuditLogger.Log("Create", "XeOTo", null,
                    "Thêm ô tô mới (qua màn hình chi tiết ô tô)");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được sửa ô tô.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 ô tô cần sửa."); return; }

            var w = new XeOToDetailWindow(row.XeOToID);
            if (w.ShowDialog() == true)
            {
                LoadData((FindName("txtSearch") as TextBox)?.Text);
                AuditLogger.Log("Update", "XeOTo", row.XeOToID.ToString(),
                    $"Sửa ô tô BKS={row.BKS}, Cư dân={row.TenCuDan}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được xóa ô tô.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 ô tô cần xóa."); return; }

            if (MessageBox.Show($"Xóa ô tô biển số [{row.BKS}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.XeOToes.FirstOrDefault(x => x.XeOToID == row.XeOToID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                db.XeOToes.Remove(entity);
                db.SaveChanges();
            }

            AuditLogger.Log("Delete", "XeOTo", row.XeOToID.ToString(),
                $"Xóa ô tô BKS={row.BKS}, Cư dân={row.TenCuDan}");

            LoadData((FindName("txtSearch") as TextBox)?.Text);
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
