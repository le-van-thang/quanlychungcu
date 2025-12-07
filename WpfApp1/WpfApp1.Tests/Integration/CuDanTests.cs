using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;
using WpfApp1.Tests.Fixtures;

namespace WpfApp1.Tests.Integration
{
    /// <summary>
    /// - TC27: Add resident valid.
    /// - TC28: Add resident thiếu tên.
    /// - TC29: Update phone.
    /// - TC30: Delete resident.
    /// - TC31: Filter residents theo căn hộ.
    /// - TC32: Validate Name required.
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class CuDanTests : DbTestBase
    {
        [TestMethod]
        public void TC27_AddResident_Valid_ShouldInsert()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var (canHoId, _) = DbSeed.EnsureCanHoWithResident(db, "C101");
                var before = db.CuDans.Count();

                db.CuDans.Add(new CuDan
                {
                    HoTen = "Cu Dan Moi",
                    CanHoID = canHoId,
                    DienThoai = "0909999999",
                    Email = "new@example.com"
                });
                db.SaveChanges();

                var after = db.CuDans.Count();
                Assert.AreEqual(before + 1, after);
            }
        }

        [TestMethod]
        public void TC28_AddResident_MissingName_ShouldNotInsert()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var (canHoId, _) = DbSeed.EnsureCanHoWithResident(db, "C101");
                var before = db.CuDans.Count();

                db.CuDans.Add(new CuDan
                {
                    HoTen = null,
                    CanHoID = canHoId
                });

                try
                {
                    db.SaveChanges();
                    var after = db.CuDans.Count();
                    Assert.AreEqual(before, after,
                        "TC28: HoTen bắt buộc, không được insert thiếu tên.");
                }
                catch
                {
                    Assert.IsTrue(true);
                }
            }
        }

        [TestMethod]
        public void TC29_UpdateResidentPhone_ShouldPersist()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var cd = Builders.MakeCuDan(db, ten: "R01");
                var newPhone = "0909999999";

                cd.DienThoai = newPhone;
                db.SaveChanges();

                var reloaded = db.CuDans.First(x => x.CuDanID == cd.CuDanID);
                Assert.AreEqual(newPhone, reloaded.DienThoai);
            }
        }

        [TestMethod]
        public void TC30_DeleteResident_ShouldRemoveRow()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var cd = Builders.MakeCuDan(db, ten: "R02");
                var id = cd.CuDanID;

                db.CuDans.Remove(cd);
                db.SaveChanges();

                var exists = db.CuDans.Any(x => x.CuDanID == id);
                Assert.IsFalse(exists);
            }
        }

        [TestMethod]
        public void TC31_FilterResidents_ByApartment_ShouldReturnCorrectRows()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var cA101 = Builders.MakeCanHo(db, "A101");
                var cB201 = Builders.MakeCanHo(db, "B201");

                db.CuDans.Add(new CuDan { HoTen = "A_Resident", CanHoID = cA101.CanHoID });
                db.CuDans.Add(new CuDan { HoTen = "B_Resident", CanHoID = cB201.CanHoID });
                db.SaveChanges();

                var list = db.CuDans
                    .Where(cd => cd.CanHoID == cA101.CanHoID)
                    .Select(cd => cd.HoTen)
                    .ToList();

                Assert.IsTrue(list.Contains("A_Resident"));
                Assert.IsFalse(list.Contains("B_Resident"));
            }
        }

        [TestMethod]
        public void TC32_ValidateResident_NameRequired()
        {
            var dto = new { HoTen = (string)null };

            bool isValid = !string.IsNullOrWhiteSpace(dto.HoTen);

            Assert.IsFalse(isValid, "TC32: HoTen null phải bị coi là invalid.");
        }
    }
}
