using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace WpfApp1
{
    public static class OtpStore
    {
        // C# 7.3: khai báo đầy đủ kiểu, không dùng "new()"
        private static readonly ConcurrentDictionary<string, Tuple<string, DateTime>> mem =
            new ConcurrentDictionary<string, Tuple<string, DateTime>>();

        // Tạo OTP 6 số bằng RNGCryptoServiceProvider (an toàn hơn Random)
        private static string NewOtp()
        {
            var bytes = new byte[4];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            // chuyển 4 byte -> int dương
            int raw = Math.Abs(BitConverter.ToInt32(bytes, 0));
            int six = (raw % 900000) + 100000; // 100000..999999
            return six.ToString();
        }

        public static string Issue(string key, TimeSpan life)
        {
            var otp = NewOtp();
            var exp = DateTime.UtcNow.Add(life);
            mem[key] = Tuple.Create(otp, exp);
            return otp;
        }

        public static bool Verify(string key, string otp)
        {
            Tuple<string, DateTime> v;
            if (!mem.TryGetValue(key, out v)) return false;
            if (DateTime.UtcNow > v.Item2) return false; // hết hạn
            bool ok = (v.Item1 == otp);
            if (ok)
            {
                Tuple<string, DateTime> _; // C# 7.3 không có discard theo kiểu var _
                mem.TryRemove(key, out _);
            }
            return ok;
        }
    }
}
