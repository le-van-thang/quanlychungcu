using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;

namespace WpfApp1.Tests.Unit
{
    /// <summary>
    /// InvoiceService Unit Test:
    /// - TC26: Unit test create (CreateInvoice_ValidData_ShouldSucceed).
    /// - TC36: Create invoice cho cư dân.
    /// - TC37: Mark invoice as paid.
    /// - TC38: Duplicate invoice same day.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class InvoiceServiceTests : DbTestBase
    {
        private QuanlychungcuEntities _db;
        private InvoiceService _svc;

        [TestInitialize]
        public void Setup()
        {
            _db = new QuanlychungcuEntities();
            DbSeed.EnsureCanHoWithResident(_db, "C101");
            _svc = new InvoiceService(_db);
        }

        /// <summary>TC26 + TC36: create invoice hợp lệ.</summary>
        [TestMethod]
        public void CreateInvoice_ValidData_ShouldSucceed()
        {
            var ok = _svc.Create("C101", 1_000_000m, "PhiQuanLy");
            Assert.IsTrue(ok);
        }

        /// <summary>Căn hộ không tồn tại.</summary>
        [TestMethod]
        public void CreateInvoice_InvalidApartment_ShouldFail()
        {
            var ok = _svc.Create("KHONG_TON_TAI", 500_000m, "PhiQuanLy");
            Assert.IsFalse(ok);
        }

        /// <summary>Số tiền không hợp lệ (0, âm).</summary>
        [TestMethod]
        public void CreateInvoice_InvalidAmount_ShouldFail()
        {
            Assert.IsFalse(_svc.Create("C101", 0m, "PhiQuanLy"));
            Assert.IsFalse(_svc.Create("C101", -1m, "PhiQuanLy"));
        }

        /// <summary>Căn hộ không có cư dân.</summary>
        [TestMethod]
        public void CreateInvoice_NoResident_ShouldFail()
        {
            var tang = _db.Tangs.First();
            var empty = new CanHo { SoCanHo = "C999", TangID = tang.TangID };
            _db.CanHoes.Add(empty);
            _db.SaveChanges();

            var ok = _svc.Create("C999", 100_000m, "PhiQuanLy");
            Assert.IsFalse(ok);
        }

        /// <summary>Loại dịch vụ không được hỗ trợ.</summary>
        [TestMethod]
        public void CreateInvoice_UnsupportedService_ShouldFail()
        {
            var ok = _svc.Create("C101", 100_000m, "KhongHoTro");
            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void CreateInvoice_DuplicateInSameDay_ShouldFail()
        {
            var ok1 = _svc.Create("C101", 100_000m, "PhiQuanLy");
            var ok2 = _svc.Create("C101", 200_000m, "PhiQuanLy");

            Assert.IsTrue(ok1);
            Assert.IsFalse(ok2);

            var today = System.DateTime.Today;
            var tomorrow = today.AddDays(1);   // ✅ tính ngoài LINQ

            int count = _db.HoaDonCuDans.Count(hd =>
                hd.LoaiDichVu == "PhiQuanLy" &&
                hd.NgayLap >= today &&
                hd.NgayLap < tomorrow);

            Assert.AreEqual(1, count);
        }

        /// <summary>TC37: Mark invoice as paid.</summary>
        [TestMethod]
        public void TC37_MarkInvoiceAsPaid_ShouldUpdateStatus()
        {
            var ok = _svc.Create("C101", 100_000m, "PhiQuanLy");
            Assert.IsTrue(ok);

            var inv = _db.HoaDonCuDans.First(h => h.CanHo.SoCanHo == "C101");
            inv.TrangThai = "Paid";
            _db.SaveChanges();

            var reloaded = _db.HoaDonCuDans.First(h => h.HoaDonID == inv.HoaDonID);
            Assert.AreEqual("Paid", reloaded.TrangThai);
        }
    }
}
