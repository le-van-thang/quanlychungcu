using System.Linq;
using System.Data.Entity;
using WpfApp1;

namespace WpfApp1.Tests.Fixtures
{
    public static class Builders
    {
        public static Tang MakeTang(QuanlychungcuEntities db, string ten = "Tầng 1")
        {
            var t = db.Tangs.FirstOrDefault();
            if (t == null)
            {
                t = db.Tangs.Add(new Tang { TenTang = ten });
                db.SaveChanges();
            }
            return t;
        }

        public static CanHo MakeCanHo(QuanlychungcuEntities db, string so = "A101",
                                      decimal dt = 60, decimal gt = 1_000_000)
        {
            var tang = MakeTang(db);
            var ch = db.Set<CanHo>().Add(new CanHo
            {
                SoCanHo = so,
                TangID = tang.TangID,
                DienTich = dt,
                GiaTri = gt
            });
            db.SaveChanges();
            return ch;
        }

        public static CuDan MakeCuDan(QuanlychungcuEntities db, int? canHoId = null,
                                      string ten = "Nguyen Van A")
        {
            var cd = db.CuDans.Add(new CuDan
            {
                HoTen = ten,
                CanHoID = canHoId,
                DienThoai = "0900000000",
                Email = "a@x.com"
            });
            db.SaveChanges();
            return cd;
        }
    }
}
