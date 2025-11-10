using System.Linq;

namespace WpfApp1.Tests.Common
{
    /// <summary>
    /// Seed dữ liệu tối thiểu để chạy test ổn định: Gieo dữ liệu mẫu ban đầu vào cơ sở dữ liệu.
    /// - Đảm bảo có 1 căn hộ (SoCanHo = tham số).
    /// - Đảm bảo căn hộ có ít nhất 1 cư dân và đánh dấu làm chủ hộ.
    /// Trả về (CanHoID, CuDanID) để test khác có thể dùng tiếp.
    /// </summary>
    public static class DbSeed
    {
        public static (int canHoId, int cuDanId) EnsureCanHoWithResident(
            QuanlychungcuEntities db, string soCanHo = "C101")
        {
            // Tạo căn hộ nếu chưa có.
            var canHo = db.CanHoes.FirstOrDefault(x => x.SoCanHo == soCanHo);
            if (canHo == null)
            {
                var tang = db.Tangs.First(); // giả định DB có sẵn ít nhất 1 tầng
                canHo = new CanHo { SoCanHo = soCanHo, TangID = tang.TangID };
                db.CanHoes.Add(canHo);
                db.SaveChanges();
            }

            // Tạo cư dân nếu căn này chưa có ai.
            var cuDan = db.CuDans.FirstOrDefault(c => c.CanHoID == canHo.CanHoID);
            if (cuDan == null)
            {
                cuDan = new CuDan { HoTen = "Chu Ho " + soCanHo, CanHoID = canHo.CanHoID };
                db.CuDans.Add(cuDan);
                db.SaveChanges();

                // Gắn quan hệ chủ hộ (để service ưu tiên đúng người).
                db.CanHo_CuDan.Add(new CanHo_CuDan { CanHoID = canHo.CanHoID, CuDanID = cuDan.CuDanID, ChuHo = true });
                db.SaveChanges();
            }

            return (canHo.CanHoID, cuDan.CuDanID);
        }
    }
}
