// AvatarStorage.cs
using System;
using System.IO;

namespace WpfApp1
{
    public static class AvatarStorage
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WpfApp1", "avatars");

        private static string PathFor(string username)
            => Path.Combine(Dir, (username ?? "unknown") + ".png");

        public static void Save(string username, byte[] pngBytes)
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(PathFor(username), pngBytes ?? new byte[0]);
        }

        public static byte[] Load(string username)
        {
            var p = PathFor(username);
            return File.Exists(p) ? File.ReadAllBytes(p) : null;
        }

        public static void Delete(string username)
        {
            var p = PathFor(username);
            if (File.Exists(p)) File.Delete(p);
        }
    }
}
