using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class VatTuControl : UserControl
    {
        private TaiKhoan _currentUser;
        private bool _isUser;   // chỉ xem

        public VatTuControl()
        {
            InitializeComponent();
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi nạp dữ liệu Vật tư:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Mở từ DashboardWindow
        public VatTuControl(TaiKhoan user) : this()
        {
            _currentUser = user;
            _isUser = RoleHelper.IsUser(user);

            ApplyRoleUi();
        }

        /// <summary>
        /// Ẩn / hiện nút theo quyền
        /// </summary>
        private void ApplyRoleUi()
        {
            if (_isUser)
            {
                // User: chỉ xem -> ẩn Thêm/Sửa/Xóa
                if (btnAdd != null) btnAdd.Visibility = Visibility.Collapsed;
                if (btnEdit != null) btnEdit.Visibility = Visibility.Collapsed;
                if (btnDelete != null) btnDelete.Visibility = Visibility.Collapsed;
            }
            // Manager/Admin: giữ nguyên
        }

        private string ConnStr => ConfigurationManager
            .ConnectionStrings["CondoDb"].ConnectionString;

        private void LoadData(string keyword = null)
        {
            var list = new List<VatTu>();

            using (var conn = new SqlConnection(ConnStr))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                var sql = "SELECT VatTuID, TenVatTu, DonVi, SoLuong, Gia, GhiChu FROM dbo.VatTu";
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    sql += " WHERE TenVatTu LIKE @kw OR DonVi LIKE @kw";
                    cmd.Parameters.AddWithValue("@kw", "%" + keyword.Trim() + "%");
                }
                sql += " ORDER BY TenVatTu";

                cmd.CommandText = sql;

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new VatTu
                        {
                            VatTuID = rd.GetInt32(0),
                            TenVatTu = rd.IsDBNull(1) ? null : rd.GetString(1),
                            DonVi = rd.IsDBNull(2) ? null : rd.GetString(2),
                            SoLuong = rd.IsDBNull(3) ? (int?)null : rd.GetInt32(3),
                            Gia = rd.IsDBNull(4) ? (decimal?)null : rd.GetDecimal(4),
                            GhiChu = rd.IsDBNull(5) ? null : rd.GetString(5)
                        });
                    }
                }
            }

            dgVatTu.ItemsSource = list;
        }

        private VatTu Current() => dgVatTu.SelectedItem as VatTu;

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var kw = txtSearch.Text;
            LoadData(kw);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                AuditLogger.Log("Search", "VatTu", null,
                    $"Tìm kiếm vật tư với từ khóa: \"{kw}\"");
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();  // Tải lại tất cả
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 vật tư."); return; }
            var w = new VatTuDetailWindow(row.VatTuID, readOnly: true);
            w.ShowDialog();

            AuditLogger.Log("View", "VatTu", row.VatTuID.ToString(),
                $"Xem vật tư: {row.TenVatTu}");
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_isUser)
            {
                MessageBox.Show("Nhóm User chỉ được xem danh sách vật tư, không được thêm.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var w = new VatTuDetailWindow(null);
            if (w.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Create", "VatTu", null,
                    "Thêm vật tư mới");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_isUser)
            {
                MessageBox.Show("Nhóm User chỉ được xem danh sách vật tư, không được sửa.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 vật tư cần sửa."); return; }
            var w = new VatTuDetailWindow(row.VatTuID);
            if (w.ShowDialog() == true)
            {
                LoadData(txtSearch.Text);
                AuditLogger.Log("Update", "VatTu", row.VatTuID.ToString(),
                    $"Sửa vật tư: {row.TenVatTu}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isUser)
            {
                MessageBox.Show("Nhóm User chỉ được xem danh sách vật tư, không được xóa.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = Current();
            if (row == null) { MessageBox.Show("Chọn 1 vật tư cần xóa."); return; }

            if (MessageBox.Show($"Xóa vật tư [{row.TenVatTu}]?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            // Check ràng buộc trước khi xóa
            if (IsReferenced(row.VatTuID))
            {
                MessageBox.Show("Vật tư đang được sử dụng (mặt bằng/căn hộ/hóa đơn). Không thể xóa.");
                return;
            }

            using (var conn = new SqlConnection(ConnStr))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "DELETE FROM dbo.VatTu WHERE VatTuID=@id";
                cmd.Parameters.AddWithValue("@id", row.VatTuID);

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không xóa được: " + ex.Message);
                    return;
                }
            }

            AuditLogger.Log("Delete", "VatTu", row.VatTuID.ToString(),
                $"Xóa vật tư: {row.TenVatTu}");

            LoadData(txtSearch.Text);
        }

        private bool IsReferenced(int vatTuId)
        {
            using (var conn = new SqlConnection(ConnStr))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT
  (SELECT COUNT(*) FROM dbo.MatBang_VatTu    WHERE VatTuID=@id) +
  (SELECT COUNT(*) FROM dbo.CanHo_VatTu      WHERE VatTuID=@id) +
  (SELECT COUNT(*) FROM dbo.HoaDonTM_ChiTiet WHERE VatTuID=@id)";
                cmd.Parameters.AddWithValue("@id", vatTuId);
                var total = (int)cmd.ExecuteScalar();
                return total > 0;
            }
        }
    }
}
