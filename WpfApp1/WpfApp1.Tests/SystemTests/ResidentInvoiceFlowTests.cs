using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;

namespace WpfApp1.Tests.SystemTests
{
    /// <summary>
    /// System test E2E cho TC23/TC24:
    /// - Lập hóa đơn qua InvoiceService rồi thử xóa Căn hộ.
    /// </summary>
    [TestClass]
    [TestCategory("System")]
    public class ResidentInvoiceFlowTests
    {
        /// <summary>
        /// TC24 (system-level): lập hóa đơn rồi xóa căn → phải bị FK chặn.
        /// </summary>
        [TestMethod]
        public void Delete_CanHo_HasInvoices_ShouldThrow()
        {
            using (var db = new QuanlychungcuEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                DbSeed.EnsureCanHoWithResident(db, "SYS-777");

                var svc = new InvoiceService(db);
                Assert.IsTrue(
                    svc.Create("SYS-777", 120_000m, "PhiQuanLy")
                );

                var canHo = db.CanHoes.First(x => x.SoCanHo == "SYS-777");

                db.CanHoes.Remove(canHo);
                DbUpdateException captured = null;
                try
                {
                    db.SaveChanges();
                    Assert.Fail("Kỳ vọng DbUpdateException nhưng SaveChanges() lại thành công.");
                }
                catch (DbUpdateException ex)
                {
                    captured = ex;
                }

                Assert.IsNotNull(captured);

                var sqlEx = captured.GetBaseException() as SqlException;
                if (sqlEx != null)
                {
                    Assert.AreEqual(547, sqlEx.Number);
                }

                db.Entry(canHo).State = EntityState.Unchanged;
                var stillThere = db.CanHoes.Any(x => x.SoCanHo == "SYS-777");
                Assert.IsTrue(stillThere);

                tx.Rollback();
            }
        }
    }
}
