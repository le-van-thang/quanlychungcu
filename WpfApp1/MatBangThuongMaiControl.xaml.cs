using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class MatBangThuongMaiControl : UserControl
    {
        private TaiKhoan _currentUser;
        private bool _isUser;

        public MatBangThuongMaiControl()
        {
            InitializeComponent();
            LoadData();
        }

        // Mở từ DashboardWindow
        public MatBangThuongMaiControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            _isUser = RoleHelper.IsUser(user);

            ApplyRoleUi();
        }

        /// <summary>
        /// Ẩn nút theo quyền
        /// </summary>
        private void ApplyRoleUi()
        {
            if (_isUser)
            {
                if (btnAdd != null) btnAdd.Visibility = Visibility.Collapsed;
                if (btnEdit != null) btnEdit.Visibility = Visibility.Collapsed;
                if (btnDelete != null) btnDelete.Visibility = Visibility.Collapsed;
            }
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
                              GiaThue = m.GiaThue,
                              GhiChu = m.GhiChu
                          });

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower();
                    q = q.Where(x =>
                        (x.TenMatBang ?? "").ToLower().Contains(keyword) ||
                        (x.GhiChu ?? "").ToLower().Contains(keyword));
                }

                dgMBTM.ItemsSource = q
                    .OrderBy(x => x.MatBangID)
                    .ToList();
            }
        }

        private MatBangRow Current() => dgMBTM.SelectedItem as MatBangRow;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var kw = txtSearch.Text;
            LoadData(kw);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                AuditLogger.Log("Search", "MatBangThuongMai", null,
                    $"Tìm kiếm mặt bằng với từ khóa: \"{kw}\"");
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadData();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null)
            {
                MessageBox.Show("Chọn 1 mặt bằng.");
                return;
            }

            var w = new MatBangThuongMaiDetailWindow(row.MatBangID, readOnly: true);
            w.ShowDialog();

            AuditLogger.Log("View", "MatBangThuongMai", row.MatBangID.ToString(),
                $"Xem mặt bằng: {row.TenMatBang}");
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_isUser)
            {
                MessageBox.Show("Nhóm User chỉ được xem danh sách mặt bằng, không được thêm.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var w = new MatBangThuongMaiDetailWindow(null);
            if (w.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Create", "MatBangThuongMai", null,
                    "Thêm mặt bằng thương mại mới");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_isUser)
            {
                MessageBox.Show("Nhóm User chỉ được xem danh sách mặt bằng, không được sửa.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null)
            {
                MessageBox.Show("Chọn 1 mặt bằng cần sửa.");
                return;
            }

            var w = new MatBangThuongMaiDetailWindow(row.MatBangID);
            if (w.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Update", "MatBangThuongMai", row.MatBangID.ToString(),
                    $"Sửa mặt bằng: {row.TenMatBang}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isUser)
            {
                MessageBox.Show("Nhóm User chỉ được xem danh sách mặt bằng, không được xóa.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null)
            {
                MessageBox.Show("Chọn 1 mặt bằng cần xóa.");
                return;
            }

            if (MessageBox.Show($"Xóa mặt bằng [{row.TenMatBang}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            using (var db = new QuanlychungcuEntities())
            {
                var entity = db.MatBangThuongMais.FirstOrDefault(x => x.MatBangID == row.MatBangID);
                if (entity == null)
                {
                    MessageBox.Show("Không tìm thấy bản ghi.");
                    return;
                }

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

            AuditLogger.Log("Delete", "MatBangThuongMai", row.MatBangID.ToString(),
                $"Xóa mặt bằng: {row.TenMatBang}");

            LoadData(txtSearch.Text);
        }
    }

    public class MatBangRow
    {
        public int MatBangID { get; set; }
        public string TenMatBang { get; set; }
        public decimal? DienTich { get; set; }
        public decimal? GiaThue { get; set; }
        public string GhiChu { get; set; }
    }
}
