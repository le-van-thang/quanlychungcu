using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;

namespace WpfApp1
{
    public partial class VatTuDetailWindow : Window
    {
        private readonly int? _id;       // null = thêm mới
        private readonly bool _readOnly; // true = chỉ xem

        public VatTuDetailWindow(int? id, bool readOnly = false)
        {
            InitializeComponent();
            _id = id;
            _readOnly = readOnly;
            Loaded += OnLoaded;
        }

        private string ConnStr => ConfigurationManager
            .ConnectionStrings["CondoDb"].ConnectionString;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_id.HasValue)
            {
                lblHeader.Text = "Thông tin vật tư (Sửa)";
                LoadEntity(_id.Value);
            }
            else
            {
                lblHeader.Text = "Thông tin vật tư (Thêm)";
                txtId.Text = "(tự tăng)";
            }

            if (_readOnly)
            {
                txtTenVatTu.IsReadOnly = true;
                txtDonVi.IsReadOnly = true;
                txtSoLuong.IsReadOnly = true;
                txtGia.IsReadOnly = true;
                txtGhiChu.IsReadOnly = true;
                btnSave.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadEntity(int id)
        {
            using (var conn = new SqlConnection(ConnStr))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT VatTuID, TenVatTu, DonVi, SoLuong, Gia, GhiChu FROM dbo.VatTu WHERE VatTuID=@id";
                cmd.Parameters.AddWithValue("@id", id);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                    {
                        MessageBox.Show("Không tìm thấy vật tư."); Close();
                        return;
                    }

                    txtId.Text = rd.GetInt32(0).ToString();
                    txtTenVatTu.Text = rd.IsDBNull(1) ? "" : rd.GetString(1);
                    txtDonVi.Text = rd.IsDBNull(2) ? "" : rd.GetString(2);
                    txtSoLuong.Text = rd.IsDBNull(3) ? "" : rd.GetInt32(3).ToString();
                    txtGia.Text = rd.IsDBNull(4) ? "" : rd.GetDecimal(4).ToString(CultureInfo.InvariantCulture);
                    txtGhiChu.Text = rd.IsDBNull(5) ? "" : rd.GetString(5);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // validate đơn giản
            if (string.IsNullOrWhiteSpace(txtTenVatTu.Text))
            {
                MessageBox.Show("Tên vật tư là bắt buộc.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? soLuong = null;
            if (!string.IsNullOrWhiteSpace(txtSoLuong.Text) &&
                int.TryParse(txtSoLuong.Text, out var s)) soLuong = s;

            decimal? gia = null;
            if (!string.IsNullOrWhiteSpace(txtGia.Text) &&
                decimal.TryParse(txtGia.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var g)) gia = g;

            var isNew = _id == null;

            try
            {
                if (isNew)
                {
                    Insert(new VatTu
                    {
                        TenVatTu = txtTenVatTu.Text.Trim(),
                        DonVi = txtDonVi.Text.Trim(),
                        SoLuong = soLuong,
                        Gia = gia,
                        GhiChu = txtGhiChu.Text.Trim()
                    });
                }
                else
                {
                    Update(new VatTu
                    {
                        VatTuID = _id.Value,
                        TenVatTu = txtTenVatTu.Text.Trim(),
                        DonVi = txtDonVi.Text.Trim(),
                        SoLuong = soLuong,
                        Gia = gia,
                        GhiChu = txtGhiChu.Text.Trim()
                    });
                }

                // ===== THÔNG BÁO THÀNH CÔNG =====
                MessageBox.Show(
                    isNew ? "Đã thêm vật tư mới thành công." : "Đã cập nhật thông tin vật tư.",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được: " + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }


        private void Insert(VatTu v)
        {
            using (var conn = new SqlConnection(ConnStr))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
INSERT INTO dbo.VatTu(TenVatTu, DonVi, SoLuong, Gia, GhiChu)
VALUES(@TenVatTu, @DonVi, @SoLuong, @Gia, @GhiChu)";
                cmd.Parameters.AddWithValue("@TenVatTu", (object)v.TenVatTu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DonVi", (object)v.DonVi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SoLuong", (object)v.SoLuong ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Gia", (object)v.Gia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", (object)v.GhiChu ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private void Update(VatTu v)
        {
            using (var conn = new SqlConnection(ConnStr))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
UPDATE dbo.VatTu
SET TenVatTu=@TenVatTu, DonVi=@DonVi, SoLuong=@SoLuong, Gia=@Gia, GhiChu=@GhiChu
WHERE VatTuID=@VatTuID";
                cmd.Parameters.AddWithValue("@VatTuID", v.VatTuID);
                cmd.Parameters.AddWithValue("@TenVatTu", (object)v.TenVatTu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DonVi", (object)v.DonVi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SoLuong", (object)v.SoLuong ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Gia", (object)v.Gia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", (object)v.GhiChu ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
