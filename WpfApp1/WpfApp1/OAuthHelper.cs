using System;
using System.Security.Cryptography;
using System.Text;

namespace WpfApp1
{
    public static class OAuthHelper
    {
        /// <summary>
        /// Sinh chuỗi ngẫu nhiên (state, nonce, code_verifier)
        /// </summary>
        public static string RandomDataBase64Url(uint length = 32)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Encode mảng byte thành Base64Url
        /// </summary>
        public static string Base64UrlEncode(byte[] input)
        {
            string s = Convert.ToBase64String(input);
            s = s.Replace("+", "-").Replace("/", "_").Replace("=", "");
            return s;
        }

        /// <summary>
        /// Tạo code challenge từ code verifier (dùng trong PKCE)
        /// </summary>
        public static string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.ASCII.GetBytes(codeVerifier);
                var hash = sha256.ComputeHash(bytes);
                return Base64UrlEncode(hash);
            }
        }
    }
}
