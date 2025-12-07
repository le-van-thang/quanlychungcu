using System;

namespace WpfApp1
{
    public static class RoleHelper
    {
        private static string Normalize(string role)
            => (role ?? "").Trim().ToLowerInvariant();

        public static bool IsAdmin(TaiKhoan tk)
        {
            var r = Normalize(tk?.VaiTro);
            return r == "admin" || r == "administrator" || r == "quantri";
        }

        public static bool IsManager(TaiKhoan tk)
        {
            var r = Normalize(tk?.VaiTro);
            return r == "manager" || r == "quanly";
        }

        public static bool IsUser(TaiKhoan tk)
        {
            // User = không phải Admin, không phải Manager
            return !IsAdmin(tk) && !IsManager(tk);
        }

        public static bool CanEditData(TaiKhoan tk)
        {
            // Quyền thêm/sửa/xóa tất cả module nghiệp vụ
            return IsAdmin(tk) || IsManager(tk);
        }

        /// <summary>
        /// Quyền quản lý ticket (xem tất cả, đổi trạng thái, gán người xử lý, xoá)
        /// </summary>
        public static bool CanManageTicket(TaiKhoan tk)
        {
            // Cho đơn giản: Admin + Manager đều quản lý ticket
            return IsAdmin(tk) || IsManager(tk);
        }
    }
}
