using System;

namespace WpfApp1
{
    public static class AuditLogger
    {
        /// <summary>
        /// Ghi nhật ký hoạt động cơ bản
        /// </summary>
        public static void Log(string action, string entityName, string entityId = null, string detail = null)
        {
            try
            {
                // Lấy UserId đang đăng nhập (có thể null nếu chưa login)
                int? userId = SessionStore.GetUserId();

                using (var db = new QuanlychungcuEntities())   // TÊN CONTEXT của EDMX
                {
                    var log = new ActivityLog
                    {
                        Action = action,
                        EntityName = entityName,
                        EntityId = entityId,
                        Detail = detail,
                        CreatedAt = DateTime.Now,
                        UserId = userId      // nullable -> OK
                    };

                    // DbSet<ActivityLog>
                    db.ActivityLogs.Add(log);   // <-- dùng Add, KHÔNG dùng AddObject
                    db.SaveChanges();
                }
            }
            catch
            {
                // Nuốt lỗi, không để crash UI vì lỗi log
                // (nếu cần thì Debug.WriteLine(ex) ở đây)
            }
        }

        // Ví dụ helper riêng cho login
        public static void LogLogin(string username, bool success, string detail = null)
        {
            string act = success ? "LoginSuccess" : "LoginFailed";
            string det = $"User={username}; {detail}";
            Log(act, "Auth", null, det);
        }
    }
}
