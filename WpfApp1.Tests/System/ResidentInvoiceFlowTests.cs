using System;
using System.Linq;
using System.Data.Entity;
using System.Data.Entity.Infrastructure; // DbUpdateException
using System.Data.SqlClient;            // SqlException 547 (nếu dùng SQL Server)
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;

namespace WpfApp1.Tests.SystemTests  // ⚠️ Đặt tên tránh đụng System của .NET
{
    /// <summary>
    /// ✅ SYSTEM TEST (luồng nghiệp vụ end-to-end, không UI):
    /// - Seed Căn hộ + Cư dân
    /// - Lập hoá đơn qua InvoiceService (đi qua "tầng nghiệp vụ")
    /// - Thử xoá Căn hộ → DB phải chặn bởi FK
    /// - Rollback cuối test để DB sạch
    /// </summary>
    [TestClass]
    public class ResidentInvoiceFlowTests
    {
        /// <summary>
        /// 🧪 Luồng E2E: Lập hoá đơn xong → xoá căn →
        /// DB phải ném DbUpdateException do ràng buộc FK.
        /// </summary>
        [TestMethod]
        public void Delete_CanHo_HasInvoices_ShouldThrow()
        {
            using (var db = new QuanlychungcuEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                // ===== Arrange (chuẩn bị như user flow) =====
                DbSeed.EnsureCanHoWithResident(db, "SYS-777");

                // Lập hoá đơn qua lớp NGHIỆP VỤ (InvoiceService) → đúng luồng hệ thống.
                var svc = new InvoiceService(db);
                // -> Mong đợi TRUE: tạo hoá đơn thành công
                Assert.IsTrue(
                    svc.Create("SYS-777", 120_000m, "PhiQuanLy"),
                    "Không lập được hoá đơn trong bước chuẩn bị dữ liệu (mong đợi TRUE)."
                );

                var canHo = db.CanHoes.First(x => x.SoCanHo == "SYS-777");

                // ===== Act =====
                // Xoá căn hộ có hoá đơn → PHẢI bị FK chặn khi SaveChanges.
                db.CanHoes.Remove(canHo);
                DbUpdateException captured = null;
                try
                {
                    db.SaveChanges();  // <- Mong đợi NÉM LỖI
                    Assert.Fail("Kỳ vọng DbUpdateException nhưng SaveChanges() lại thành công.");
                }
                catch (DbUpdateException ex)
                {
                    captured = ex;     // Giữ lại để assert chi tiết
                }

                // ===== Assert =====
                // 1) Bắt buộc có DbUpdateException (vi phạm FK) → test PASS
                Assert.IsNotNull(captured,
                    "Phải ném DbUpdateException khi vi phạm FK (mong đợi captured != null).");

                // 2) (Nếu SQL Server) kiểm tra mã 547 cho chắc chắn
                var sqlEx = captured.GetBaseException() as SqlException;
                if (sqlEx != null)
                {
                    Assert.AreEqual(547, sqlEx.Number,
                        "Mong đợi mã lỗi SQL 547 (vi phạm FK) trên SQL Server.");
                }

                // 3) Sau lỗi, căn hộ vẫn CÒN (xoá không thành công) → mong đợi TRUE
                db.Entry(canHo).State = EntityState.Unchanged;
                var stillThere = db.CanHoes.Any(x => x.SoCanHo == "SYS-777");
                Assert.IsTrue(stillThere,
                    "Căn hộ vẫn phải tồn tại vì xoá đã bị FK chặn (mong đợi TRUE).");

                // Rollback để hoàn tác dữ liệu demo
                tx.Rollback();
            }
        }
    }
}
