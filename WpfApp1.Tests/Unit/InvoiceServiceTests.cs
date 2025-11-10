using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using WpfApp1;
using WpfApp1.Tests.Common;

namespace WpfApp1.Tests.Unit
{
    /// <summary>
    /// Kiểm thử cấp đơn vị (Unit) cho InvoiceService:
    /// - Happy path (tạo thành công).
    /// - Căn hộ không tồn tại.
    /// - Số tiền không hợp lệ.
    /// - Căn hộ không có cư dân.
    /// - Loại dịch vụ không hỗ trợ.
    /// - Trùng hóa đơn trong cùng ngày.
    /// </summary>
    [TestClass]
    public class InvoiceServiceTests : DbTestBase
    {
        private QuanlychungcuEntities _db;
        private InvoiceService _svc;

        [TestInitialize]
        public void Setup()
        {
            // Mỗi test dùng DbContext mới cho sạch sẽ.
            _db = new QuanlychungcuEntities();

            // Đảm bảo có căn hộ + cư dân mặc định C101 để dùng cho case thành công.
            DbSeed.EnsureCanHoWithResident(_db, "C101");

            _svc = new InvoiceService(_db);
        }

        [TestMethod]
        public void CreateInvoice_ValidData_ShouldSucceed()
        {
            // ✅ Test: đường đi "happy path".
            // Điều kiện: C101 có thật & có cư dân; số tiền > 0; loại DV hợp lệ.
            var ok = _svc.Create("C101", 1_000_000m, "PhiQuanLy");
            Assert.IsTrue(ok); // mong đợi: thành công
        }

        [TestMethod]
        public void CreateInvoice_InvalidApartment_ShouldFail()
        {
            // ❌ Test: căn hộ KHÔNG tồn tại.
            // Mong đợi: service trả FALSE vì không tìm thấy căn hộ để gán hóa đơn.
            var ok = _svc.Create("KHONG_TON_TAI", 500_000m, "PhiQuanLy");
            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void CreateInvoice_InvalidAmount_ShouldFail()
        {
            // ❌ Test: số tiền không hợp lệ (0 hoặc âm).
            // Mong đợi: đều FALSE vì vi phạm rule soTien > 0.
            Assert.IsFalse(_svc.Create("C101", 0m, "PhiQuanLy"));
            Assert.IsFalse(_svc.Create("C101", -1m, "PhiQuanLy"));
        }

        [TestMethod]
        public void CreateInvoice_NoResident_ShouldFail()
        {
            // ❌ Test: căn hộ có thật nhưng KHÔNG có cư dân → không thể lập hóa đơn.
            // Tạo 1 căn hộ rỗng.
            var tang = _db.Tangs.First();
            var empty = new CanHo { SoCanHo = "C999", TangID = tang.TangID };
            _db.CanHoes.Add(empty);
            _db.SaveChanges();

            var ok = _svc.Create("C999", 100_000m, "PhiQuanLy");
            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void CreateInvoice_UnsupportedService_ShouldFail()
        {
            // ❌ Test: loại dịch vụ không nằm trong danh sách cho phép (_allowedServices).
            // Mong đợi: FALSE.
            var ok = _svc.Create("C101", 100_000m, "KhongHoTro");
            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void CreateInvoice_DuplicateInSameDay_ShouldFail()
        {
            // ❌ Test: cấm tạo TRÙNG hóa đơn trong CÙNG NGÀY (cùng căn + cùng cư dân + cùng loại DV).
            // Bước 1: tạo lần 1 → OK.
            var ok1 = _svc.Create("C101", 100_000m, "PhiQuanLy");
            // Bước 2: tạo lần 2 trong cùng ngày → phải FAIL.
            var ok2 = _svc.Create("C101", 200_000m, "PhiQuanLy");

            Assert.IsTrue(ok1);
            Assert.IsFalse(ok2);

            // Kiểm tra DB: hôm nay chỉ có 1 hóa đơn loại "PhiQuanLy".
            var today = System.DateTime.Today;
            int count = _db.HoaDonCuDans.Count(hd =>
                hd.LoaiDichVu == "PhiQuanLy" &&
                hd.NgayLap >= today && hd.NgayLap < today.AddDays(1));
            Assert.AreEqual(1, count);
        }
    }
}
