using System;

namespace WpfApp1.Models
{
    public class OAuthUserInfo
    {
        public string ProviderName { get; set; }   // "Google" | "Facebook"
        public string ProviderUserId { get; set; } // Google: sub/id, Facebook: id
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }   // Google có, FB thường không
        public DateTime? ExpiresAt { get; set; }   // hết hạn token (nếu có)
    }
}
