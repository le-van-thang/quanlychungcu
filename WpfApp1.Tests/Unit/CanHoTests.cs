using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;
using WpfApp1.Tests.Fixtures;

namespace WpfApp1.Tests.Unit
{
    [TestClass]
    public class CanHoTests : DbTestBase
    {
        [TestMethod]
        public void CanHo_Create_Valid()
        {
            using (var db = new QuanlychungcuEntities())
            {
                // Tạo 1 căn hộ mới bằng Builders → đếm số dòng tăng +1 → pass.
                var before = db.CanHoes.Count();
                Builders.MakeCanHo(db, "UT-101");
                var after = db.CanHoes.Count();
                Assert.AreEqual(before + 1, after);
            }
        }
    }
}
