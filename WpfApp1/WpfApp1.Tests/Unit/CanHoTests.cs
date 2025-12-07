using System.Diagnostics;
using System.Linq;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;
using WpfApp1.Tests.Fixtures;

namespace WpfApp1.Tests.Unit
{
    /// <summary>
    /// Bao phủ:
    /// - TC19: Tạo căn hợp lệ.
    /// - TC20: Tạo căn tên rỗng.
    /// - TC21: Trùng SoCanHo.
    /// - TC25: Search căn theo keyword.
    /// - TC50: Boundary chiều dài tên căn.
    /// - TC51: Performance 500+ bản ghi.
    /// - TC52: Concurrent insert cùng SoCanHo.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class CanHoTests : DbTestBase
    {
        [TestMethod]
        public void TC19_CreateApartment_Valid_ShouldIncreaseCount()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var before = db.CanHoes.Count();
                Builders.MakeCanHo(db, "UT-101");
                var after = db.CanHoes.Count();
                Assert.AreEqual(before + 1, after);
            }
        }

        [TestMethod]
        public void TC20_CreateApartment_EmptyName_ShouldNotInsert()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var tang = Builders.MakeTang(db);
                var before = db.CanHoes.Count();

                db.CanHoes.Add(new CanHo
                {
                    SoCanHo = "",
                    TangID = tang.TangID
                });

                try
                {
                    db.SaveChanges();
                    var after = db.CanHoes.Count();
                    Assert.AreEqual(before, after,
                        "TC20: Không được insert căn có SoCanHo rỗng.");
                }
                catch (DbUpdateException)
                {
                    Assert.IsTrue(true);
                }
            }
        }

        [TestMethod]
        public void TC21_CreateApartment_DuplicateCode_ShouldBeRejected()
        {
            using (var db = new QuanlychungcuEntities())
            {
                Builders.MakeCanHo(db, "A101");
                var before = db.CanHoes.Count();

                db.CanHoes.Add(new CanHo
                {
                    SoCanHo = "A101",
                    TangID = db.Tangs.First().TangID
                });

                try
                {
                    db.SaveChanges();
                    var after = db.CanHoes.Count();
                    Assert.AreEqual(before, after,
                        "TC21: Không được có 2 căn cùng SoCanHo=A101.");
                }
                catch (DbUpdateException)
                {
                    Assert.IsTrue(true);
                }
            }
        }

        [TestMethod]
        public void TC25_SearchApartment_ByKeyword_ShouldFilterCorrectly()
        {
            using (var db = new QuanlychungcuEntities())
            {
                Builders.MakeCanHo(db, "A101");
                Builders.MakeCanHo(db, "B201");

                var result = db.CanHoes
                    .Where(ch => ch.SoCanHo.Contains("A1"))
                    .Select(ch => ch.SoCanHo)
                    .ToList();

                Assert.IsTrue(result.Contains("A101"));
                Assert.IsFalse(result.Contains("B201"));
            }
        }

        [TestMethod]
        public void TC50_ApartmentName_MaxLength_Boundary()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var tang = Builders.MakeTang(db);
                string maxOk = new string('A', 50);
                string tooLong = new string('B', 51);

                db.CanHoes.Add(new CanHo { SoCanHo = maxOk, TangID = tang.TangID });
                db.SaveChanges(); // 50 ký tự: OK

                db.CanHoes.Add(new CanHo { SoCanHo = tooLong, TangID = tang.TangID });

                try
                {
                    db.SaveChanges();
                    Assert.Fail("TC50: tên 51 ký tự đáng lẽ bị chặn.");
                }
                catch (DbUpdateException)
                {
                    Assert.IsTrue(true);
                }
            }
        }

        [TestMethod]
        public void TC51_ApartmentList_Performance_500Rows_Under2Seconds()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var tang = Builders.MakeTang(db);

                for (int i = 0; i < 500; i++)
                {
                    db.CanHoes.Add(new CanHo
                    {
                        SoCanHo = "PF-" + i,
                        TangID = tang.TangID
                    });
                }
                db.SaveChanges();

                var sw = Stopwatch.StartNew();
                var list = db.CanHoes.AsNoTracking().ToList();
                sw.Stop();

                Assert.IsTrue(sw.ElapsedMilliseconds < 2000,
                    $"TC51: Load 500+ căn mất {sw.ElapsedMilliseconds} ms > 2000 ms.");
            }
        }

        [TestMethod]
        public void TC52_ConcurrentInsert_SameApartmentCode_OnlyOneRecord()
        {
            const string code = "CONCURRENT101";

            using (var db1 = new QuanlychungcuEntities())
            using (var db2 = new QuanlychungcuEntities())
            {
                var tang1 = db1.Tangs.First();
                var tang2 = db2.Tangs.First();

                db1.CanHoes.Add(new CanHo { SoCanHo = code, TangID = tang1.TangID });
                db2.CanHoes.Add(new CanHo { SoCanHo = code, TangID = tang2.TangID });

                db1.SaveChanges();

                try
                {
                    db2.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    // OK: DB enforce unique
                }
            }

            using (var checkDb = new QuanlychungcuEntities())
            {
                int count = checkDb.CanHoes.Count(ch => ch.SoCanHo == code);
                Assert.AreEqual(1, count, "TC52: Chỉ được tối đa 1 bản ghi với SoCanHo trùng.");
            }
        }
    }
}
