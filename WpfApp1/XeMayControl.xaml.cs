using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class XeMayControl : UserControl
    {
        private TaiKhoan _currentUser;

        public XeMayControl()
        {
            InitializeComponent();
            LoadData();
        }

        public XeMayControl(TaiKhoan user) : this()
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

        private XeMayRow Current()
            => (FindName("dgXeMay") as DataGrid)?.SelectedItem as XeMayRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var txt = FindName("txtSearch") as TextBox;
            var kw = txt?.Text;
            LoadData(kw);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                AuditLogger.Log("Search", "XeMay", null,
                    $"Tìm kiếm xe máy với từ khóa: \"{kw}\"");
            }
        }

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

            AuditLogger.Log("View", "XeMay", row.XeMayID.ToString(),
                $"Xem xe máy BKS={row.BKS}, Cư dân={row.TenCuDan}");
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được thêm xe máy.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var w = new XeMayDetailWindow(null);
            if (w.ShowDialog() == true)
            {
                LoadData((FindName("txtSearch") as TextBox)?.Text);
                AuditLogger.Log("Create", "XeMay", null,
                    "Thêm xe máy mới (qua màn hình chi tiết xe máy)");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được sửa xe máy.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe máy cần sửa."); return; }

            var w = new XeMayDetailWindow(row.XeMayID);
            if (w.ShowDialog() == true)
            {
                LoadData((FindName("txtSearch") as TextBox)?.Text);
                AuditLogger.Log("Update", "XeMay", row.XeMayID.ToString(),
                    $"Sửa xe máy BKS={row.BKS}, Cư dân={row.TenCuDan}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được xóa xe máy.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe máy cần xóa."); return; }

            if (MessageBox.Show($"Xóa xe máy biển số [{row.BKS}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.XeMays.FirstOrDefault(x => x.XeMayID == row.XeMayID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                db.XeMays.Remove(entity);
                db.SaveChanges();
            }

            AuditLogger.Log("Delete", "XeMay", row.XeMayID.ToString(),
                $"Xóa xe máy BKS={row.BKS}, Cư dân={row.TenCuDan}");

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
