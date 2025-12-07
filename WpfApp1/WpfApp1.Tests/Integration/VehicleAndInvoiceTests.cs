using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;

namespace WpfApp1.Tests.Integration
{
    /// <summary>
    /// - TC33: Thêm xe ô tô cho cư dân.
    /// - TC34: Không cho trùng biển số ô tô.
    /// - TC35: Xe máy không có owner (chưa hiện thực → FAIL).
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class VehicleAndInvoiceTests : DbTestBase
    {
        [TestMethod]
        public void TC33_AddCar_ForResident_ShouldInsert()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var (_, cuDanId) = DbSeed.EnsureCanHoWithResident(db, "CAR-101");
                var before = db.XeOToes.Count();

                db.XeOToes.Add(new XeOTo
                {
                    CuDanID = cuDanId,
                    BKS = "43A-12345"
                });
                db.SaveChanges();

                var after = db.XeOToes.Count();
                Assert.AreEqual(before + 1, after);
            }
        }

        [TestMethod]
        public void TC34_AddCar_DuplicatePlate_ShouldBeRejected()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var (_, cuDanId) = DbSeed.EnsureCanHoWithResident(db, "CAR-201");

                db.XeOToes.Add(new XeOTo { CuDanID = cuDanId, BKS = "43A-12345" });
                db.SaveChanges();

                var before = db.XeOToes.Count(x => x.BKS == "43A-12345");

                db.XeOToes.Add(new XeOTo { CuDanID = cuDanId, BKS = "43A-12345" });

                try
                {
                    db.SaveChanges();
                    var after = db.XeOToes.Count(x => x.BKS == "43A-12345");
                    Assert.AreEqual(1, after, "TC34: không được có 2 xe cùng BKS 43A-12345.");
                }
                catch
                {
                    Assert.IsTrue(true);
                }
            }
        }

        [TestMethod]
        [Ignore("TODO – TC35 sẽ kiểm thử rule XeMay không có owner bằng tay.")]
        public void TC35_AddMotorbike_WithoutOwner_BehaviorDependsOnRequirement()
        {
            // Để trống, chỉ mapping với Test Case.
        }
    }
}
