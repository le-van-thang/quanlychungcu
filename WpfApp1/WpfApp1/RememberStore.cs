using System;
using System.IO;

namespace WpfApp1
{
    public static class RememberStore
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "WpfApp1", "remember");

        private static readonly string UserFile = Path.Combine(Dir, "user.txt");
        private const string PwKey = "__remember__"; // khóa ảo để lưu bằng SecureVault

        public static void Save(string user, string pass)
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(UserFile, user ?? "");
            SecureVault.SavePassword(PwKey, pass ?? "");
        }

        public static (string user, string pass)? Load()
        {
            if (!File.Exists(UserFile)) return null;
            var u = File.ReadAllText(UserFile);
            var p = SecureVault.LoadPassword(PwKey);
            return (u, p ?? "");
        }

        public static void Clear()
        {
            if (File.Exists(UserFile)) File.Delete(UserFile);
            SecureVault.DeletePassword(PwKey);
        }
    }
}
