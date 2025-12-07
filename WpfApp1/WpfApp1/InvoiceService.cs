using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    /// <summary>
    /// 🧾 Lớp nghiệp vụ tạo hóa đơn cư dân.
    /// - Input: số căn hộ (SoCanHo), số tiền, loại dịch vụ.
    /// - Mục tiêu: đảm bảo tạo hóa đơn hợp lệ, không trùng trong cùng ngày,
    ///   và có cư dân hợp lệ trong căn hộ.
    /// </summary>
    public class InvoiceService
    {
        // Biến chứa DbContext (EF6) để thao tác với database.
        private readonly QuanlychungcuEntities _db;

        // Danh sách loại dịch vụ được phép.
        private static readonly HashSet<string> _allowedServices =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "PhiQuanLy", "TienXe", "TienNuoc", "TienDien" };

        /// <summary>
        /// Constructor — cho phép Dependency Injection để test dễ hơn.
        /// Nếu không truyền gì, sẽ tự khởi tạo context thật.
        /// </summary>
        public InvoiceService(QuanlychungcuEntities db = null)
        {
            _db = db ?? new QuanlychungcuEntities();
        }

        /// <summary>
        /// 🧮 Hàm tạo hóa đơn cư dân.
        /// - Bước 1: kiểm tra input hợp lệ.
        /// - Bước 2: tìm căn hộ và cư dân tương ứng.
        /// - Bước 3: kiểm tra trùng hóa đơn trong cùng ngày.
        /// - Bước 4: tạo mới bản ghi hóa đơn.
        /// </summary>
        /// <param name="soCanHo">Mã số căn hộ (vd: "C101")</param>
        /// <param name="soTien">Số tiền cần lập hóa đơn</param>
        /// <param name="loaiDichVu">Tên loại dịch vụ (vd: "PhiQuanLy")</param>
        /// <returns>True nếu tạo thành công, False nếu lỗi</returns>
        public bool Create(string soCanHo, decimal soTien, string loaiDichVu = "PhiQuanLy")
        {
            // ====== Kiểm tra đầu vào ======
            if (string.IsNullOrWhiteSpace(soCanHo)) return false;      // thiếu mã căn hộ
            if (soTien <= 0) return false;                            // số tiền không hợp lệ
            if (!_allowedServices.Contains(loaiDichVu)) return false; // loại dịch vụ không hợp lệ

            // ====== Tìm căn hộ ======
            var canHo = _db.CanHoes.FirstOrDefault(x => x.SoCanHo == soCanHo);
            if (canHo == null) return false; // không có căn hộ này

            // ====== Lấy cư dân: ưu tiên chủ hộ, nếu không có thì lấy cư dân đầu tiên ======
            var chuHoId = _db.CanHo_CuDan
                .Where(x => x.CanHoID == canHo.CanHoID && x.ChuHo == true)
                .Select(x => (int?)x.CuDanID)
                .FirstOrDefault();

            var cuDanId = chuHoId ?? _db.CuDans
                .Where(c => c.CanHoID == canHo.CanHoID)
                .Select(c => (int?)c.CuDanID)
                .FirstOrDefault();

            if (cuDanId == null) return false; // căn hộ chưa có cư dân → không thể lập hóa đơn

            // ====== Kiểm tra trùng hóa đơn trong cùng ngày ======
            // Nếu đã có hóa đơn cùng loại DV hôm nay cho cùng căn hộ & cư dân → return false
            var today = DateTime.Today;
            bool existed = _db.HoaDonCuDans.Any(inv =>
                inv.CanHoID == canHo.CanHoID &&
                inv.CuDanID == cuDanId.Value &&
                inv.LoaiDichVu == loaiDichVu &&
                inv.NgayLap >= today && inv.NgayLap < today.AddDays(1));

            if (existed) return false;

            // ====== Tạo hóa đơn mới ======
            var hoaDon = new HoaDonCuDan
            {
                CuDanID = cuDanId.Value,
                CanHoID = canHo.CanHoID,
                LoaiDichVu = loaiDichVu,
                SoTien = soTien,
                TrangThai = "Pending", // hóa đơn mới lập → chưa thanh toán
                NgayLap = DateTime.Now
            };

            // Lưu xuống cơ sở dữ liệu
            _db.HoaDonCuDans.Add(hoaDon);
            _db.SaveChanges();

            return true;
        }
    }
}
