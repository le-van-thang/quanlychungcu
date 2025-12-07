using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WpfApp1
{
    /// <summary>
    /// 1) DPAPI cho "Ghi nhớ" (Save/Load/DeletePassword)
    /// 2) PBKDF2 cho DB (HashPassword/VerifyPassword)
    /// Format DB: iterations.saltBase64.hashBase64
    /// </summary>
    public static class SecureVault
    {
        // ===== DPAPI: Remember me =====
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WpfApp1", "vault");

        private static string PathFor(string username) { return Path.Combine(Dir, username + ".bin"); }

        public static void SavePassword(string username, string password)
        {
            Directory.CreateDirectory(Dir);
            var data = Encoding.UTF8.GetBytes(password ?? "");
            var enc = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(PathFor(username), enc);
        }

        public static string LoadPassword(string username)
        {
            var p = PathFor(username);
            if (!File.Exists(p)) return null;
            var enc = File.ReadAllBytes(p);
            var dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dec);
        }

        public static void DeletePassword(string username)
        {
            var p = PathFor(username);
            if (File.Exists(p)) File.Delete(p);
        }

        // ===== PBKDF2: DB =====
        private const int DefaultIterations = 50000;
        private const int SaltSize = 16; // 128-bit
        private const int KeySize = 32;  // 256-bit

        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException("password");

            var salt = new byte[SaltSize];
            // C# 7.3 compatible
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var hash = Pbkdf2(password, salt, DefaultIterations, KeySize);

            return DefaultIterations.ToString() + "." +
                   Convert.ToBase64String(salt) + "." +
                   Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return false;

            var parts = stored.Split('.');
            if (parts.Length != 3) return false;

            int iterations;
            if (!int.TryParse(parts[0], out iterations)) return false;

            byte[] salt, hash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                hash = Convert.FromBase64String(parts[2]);
            }
            catch
            {
                return false;
            }

            var test = Pbkdf2(password, salt, iterations, hash.Length);
            return SlowEquals(hash, test);
        }

        private static byte[] Pbkdf2(string password, byte[] salt, int iterations, int length)
        {
            // Dùng using (...) thay vì "using var"
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations)) // HMACSHA1 mặc định trên .NET Fx
            {
                return pbkdf2.GetBytes(length);
            }
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
