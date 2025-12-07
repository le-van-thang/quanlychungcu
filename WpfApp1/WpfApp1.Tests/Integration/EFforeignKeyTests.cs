using System;
using System.Linq;
using System.Data.Entity;                // EntityState, transaction
using System.Data.Entity.Infrastructure; // DbUpdateException (lỗi khi SaveChanges)
using System.Data.SqlClient;             // SqlException (để đọc mã lỗi SQL 547)
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;
//Rollback nghĩa là “hoàn tác thay đổi dữ liệu” — tức là mọi dữ liệu bạn thêm, sửa, xóa trong test sẽ tự động bị xóa đi sau khi test kết thúc
namespace WpfApp1.Tests.Integration
{
    /// <summary>
    /// ✅ INTEGRATION TEST (EF6 ↔ SQL – tập trung tầng dữ liệu)
    /// - Mục tiêu: Ràng buộc khóa ngoại (FK) CHẶN việc xoá Căn hộ đã có Hoá đơn.
    /// - Dùng transaction để ROLLBACK (không để lại dữ liệu rác sau test).
    /// - Nếu là SQL Server: InnerException có thể là SqlException với mã 547 (FK violation).
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    /// <summary>
    /// TC23–TC24: kiểm tra ràng buộc FK CanHo – HoaDonCuDan.
    /// </summary>
    public class EFforeignKeyTests
    {
        /// <summary>
        /// 🧪 Ca 1 – Xoá Căn hộ đang có hoá đơn → PHẢI BỊ CHẶN.
        ///
        /// Kết quả mong đợi:
        /// - SaveChanges() NÉM DbUpdateException (→ test PASS).
        /// - (Nếu SQL Server) SqlException.Number == 547.
        /// - Sau khi bị chặn, bản ghi Căn hộ vẫn CÒN trong DB.
        /// </summary>
        [TestMethod]
        /// <summary>
        /// TC24: Prevent deletion of apartment with residents/invoices.
        /// Xóa căn hộ đã có hóa đơn → phải bị FK chặn.
        /// </summary>
        public void Delete_CanHo_HasInvoices_ShouldThrow()
        {
            using (var db = new QuanlychungcuEntities())
            using (var tx = db.Database.BeginTransaction())  // rollback sau test
            {
                // ===== Arrange (chuẩn bị dữ liệu) =====
                // Đảm bảo có sẵn 1 Căn hộ + 1 Cư dân để test (mã "C777").
                // Hàm seed sẽ tạo mới nếu thiếu.
                DbSeed.EnsureCanHoWithResident(db, "C777");

                // Lập 1 hoá đơn để tạo quan hệ ràng buộc FK tới Căn hộ "C777".
                var svc = new InvoiceService(db);
                // -> Mong đợi TRUE (lập hoá đơn thành công)
                Assert.IsTrue(svc.Create("C777", 50_000m, "TienXe"),
                    "Không lập được hoá đơn chuẩn bị dữ liệu – cần có hoá đơn để ràng buộc FK.");

                var canHo = db.CanHoes.First(x => x.SoCanHo == "C777");

                // ===== Act (thực thi hành vi cần kiểm tra) =====
                // Thử xoá Căn hộ đang có hoá đơn → phải bị FK chặn.
                db.CanHoes.Remove(canHo);
                DbUpdateException captured = null;
                try
                {
                    db.SaveChanges();  // <- Mong đợi NÉM LỖI
                    // Nếu chạy đến đây tức là KHÔNG ném lỗi → test phải FAIL.
                    Assert.Fail("Kỳ vọng DbUpdateException nhưng SaveChanges() lại thành công.");
                }
                catch (DbUpdateException ex)
                {
                    captured = ex;     // Giữ lại exception để assert chi tiết bên dưới
                }

                // ===== Assert (xác nhận kết quả) =====
                // 1) BẮT BUỘC phải có DbUpdateException -> test PASS nếu captured != null
                Assert.IsNotNull(captured,
                    "Phải ném DbUpdateException khi vi phạm FK (xoá căn có hoá đơn).");

                // 2) (Tuỳ RDBMS) Nếu là SQL Server, mã lỗi FK thường là 547.
                var sqlEx = captured.GetBaseException() as SqlException;
                if (sqlEx != null)
                {
                    Assert.AreEqual(547, sqlEx.Number,
                        "Mong đợi mã lỗi SQL 547 (vi phạm FK) trên SQL Server.");
                }

                // 3) Sau khi bị chặn, Căn hộ vẫn còn trong DB (xoá KHÔNG thành công).
                //    -> TRUE = bản ghi còn tồn tại; FALSE = mất bản ghi (không đúng mong đợi).
                db.Entry(canHo).State = EntityState.Unchanged; // dọn trạng thái tracking
                var stillThere = db.CanHoes.Any(x => x.SoCanHo == "C777");
                Assert.IsTrue(stillThere,
                    "Căn hộ vẫn phải tồn tại vì xoá đã bị FK chặn (mong đợi TRUE).");

                // Luôn rollback để không bẩn dữ liệu thật.
                tx.Rollback();
            }
        }

        /// <summary>
        /// 🧪 Ca 2 – Xoá Căn hộ KHÔNG có hoá đơn → CHO PHÉP.
        ///
        /// Kết quả mong đợi:
        /// - SaveChanges() KHÔNG ném lỗi (→ test tiếp tục chạy).
        /// - Sau khi SaveChanges, bản ghi Căn hộ phải MẤT (không còn tồn tại).
        /// </summary>
        [TestMethod]
        /// TC23: Delete apartment without residents/invoices → cho phép.
        public void Delete_CanHo_NoInvoices_ShouldSucceed()
        {
            using (var db = new QuanlychungcuEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                // ===== Arrange =====
                // Tạo 1 Căn hộ mới "INTEG-NO-INV" không có hoá đơn nào ràng vào.
                var tang = db.Tangs.First(); // giả định DB có sẵn ít nhất 1 tầng
                const string id = "INTEG-NO-INV";
                if (!db.CanHoes.Any(x => x.SoCanHo == id))
                {
                    db.CanHoes.Add(new CanHo { SoCanHo = id, TangID = tang.TangID });
                    db.SaveChanges();
                }

                var canHo = db.CanHoes.First(x => x.SoCanHo == id);

                // Bảo đảm không có hoá đơn nào của căn này (mong đợi FALSE).
                var hasInvoices = db.HoaDonCuDans.Any(hd => hd.CanHoID == canHo.CanHoID);
                Assert.IsFalse(hasInvoices,
                    "Dữ liệu chuẩn bị phải đảm bảo căn hộ không có hoá đơn (mong đợi FALSE).");

                // ===== Act =====
                db.CanHoes.Remove(canHo);
                db.SaveChanges(); // <- KHÔNG mong đợi exception

                // ===== Assert =====
                // Sau khi xoá, căn hộ phải biến mất (mong đợi FALSE khi truy vấn tồn tại).
                var stillThere = db.CanHoes.Any(x => x.SoCanHo == id);
                Assert.IsFalse(stillThere,
                    "Căn hộ phải bị xoá khi không có hoá đơn ràng buộc (mong đợi FALSE).");

                tx.Rollback(); // giữ DB sạch
            }
        }
    }
}
