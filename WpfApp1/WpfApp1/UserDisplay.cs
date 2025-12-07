namespace WpfApp1.Utils
{
    public static class UserDisplay
    {
        /// <summary>Trả về tên hiển thị “đẹp” cho một tài khoản.</summary>
        public static string Get(TaiKhoan tk)
        {
            if (tk == null) return "(unknown)";

            // Ưu tiên các cột tên trong TaiKhoan
            var name = tk.GetType().GetProperty("HoTen")?.GetValue(tk)?.ToString();
            if (string.IsNullOrWhiteSpace(name))
                name = tk.GetType().GetProperty("FullName")?.GetValue(tk)?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();

            // Rớt về Email nếu có
            var email = tk.GetType().GetProperty("Email")?.GetValue(tk)?.ToString();
            if (!string.IsNullOrWhiteSpace(email))
                return email.Trim();

            // Xử lý username dạng "Facebook:123..." => cắt prefix
            var username = tk.GetType().GetProperty("Username")?.GetValue(tk)?.ToString() ?? "";
            if (username.StartsWith("Facebook:", System.StringComparison.OrdinalIgnoreCase))
                return username.Substring("Facebook:".Length);
            if (username.StartsWith("Google:", System.StringComparison.OrdinalIgnoreCase))
                return username.Substring("Google:".Length);

            return string.IsNullOrWhiteSpace(username) ? "(unknown)" : username.Trim();
        }
    }
}
