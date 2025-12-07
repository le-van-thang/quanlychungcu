using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class XeDapControl : UserControl
    {
        private TaiKhoan _currentUser;

        public XeDapControl()
        {
            InitializeComponent();
            LoadData();
        }

        public XeDapControl(TaiKhoan user) : this()
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
        {
            var txt = FindName("txtSearch") as TextBox;
            var kw = txt?.Text;
            LoadData(kw);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                AuditLogger.Log("Search", "XeDap", null,
                    $"Tìm kiếm xe đạp với từ khóa: \"{kw}\"");
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
            if (row == null) { MessageBox.Show("Chọn 1 xe đạp."); return; }
            new XeDapDetailWindow(row.XeDapID, readOnly: true).ShowDialog();

            AuditLogger.Log("View", "XeDap", row.XeDapID.ToString(),
                $"Xem xe đạp BKS={row.BKS}, Cư dân={row.TenCuDan}");
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được thêm xe đạp.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var w = new XeDapDetailWindow(null);
            if (w.ShowDialog() == true)
            {
                LoadData((FindName("txtSearch") as TextBox)?.Text);
                AuditLogger.Log("Create", "XeDap", null,
                    "Thêm xe đạp mới (qua màn hình chi tiết xe đạp)");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được sửa xe đạp.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe cần sửa."); return; }

            var w = new XeDapDetailWindow(row.XeDapID);
            if (w.ShowDialog() == true)
            {
                LoadData((FindName("txtSearch") as TextBox)?.Text);
                AuditLogger.Log("Update", "XeDap", row.XeDapID.ToString(),
                    $"Sửa xe đạp BKS={row.BKS}, Cư dân={row.TenCuDan}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                MessageBox.Show("Chỉ Manager/Admin được xóa xe đạp.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 xe cần xóa."); return; }

            if (MessageBox.Show($"Xóa xe đạp biển số [{row.BKS}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.XeDaps.FirstOrDefault(x => x.XeDapID == row.XeDapID);
                if (entity == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                db.XeDaps.Remove(entity);
                db.SaveChanges();
            }

            AuditLogger.Log("Delete", "XeDap", row.XeDapID.ToString(),
                $"Xóa xe đạp BKS={row.BKS}, Cư dân={row.TenCuDan}");

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
