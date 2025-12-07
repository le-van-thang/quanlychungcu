using System;
using System.IO;

namespace WpfApp1
{
    public static class SessionStore
    {
        private static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuanLyChungCuApp");

        private static readonly string FilePath = Path.Combine(Folder, "session.txt");

        public static void Save(int userId)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(FilePath, userId.ToString());
            }
            catch { }
        }

        public static int? Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var text = File.ReadAllText(FilePath).Trim();
                if (int.TryParse(text, out var id)) return id;
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch { }
        }

        // Thêm hàm tiện dụng
        public static int? GetUserId() => Load();
    }
}
