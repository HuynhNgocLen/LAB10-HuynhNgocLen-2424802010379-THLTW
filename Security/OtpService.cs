using System;
using System.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HuynhNgocLen.SachOnline.Security
{
    /// <summary>Mã OTP số (độ dài cấu hình OtpCodeLength trong Web.config, 4–8) và kiểm tra an toàn.</summary>
    public static class OtpService
    {
        private static readonly Lazy<int> CodeLengthLazy = new Lazy<int>(() =>
        {
            var raw = ConfigurationManager.AppSettings["OtpCodeLength"];
            if (int.TryParse(raw, out var n) && n >= 4 && n <= 8)
                return n;
            return 8;
        });

        /// <summary>Độ dài mã (mặc định 8 nếu không cấu hình hoặc không hợp lệ).</summary>
        public static int CodeLength => CodeLengthLazy.Value;

        public static string GenerateNumericCode()
        {
            var len = CodeLength;
            uint max = 1;
            for (var i = 0; i < len; i++)
                max *= 10u;

            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var n = BitConverter.ToUInt32(bytes, 0) % max;
                return n.ToString("D" + len.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            }
        }

        public static string HashCode(string code, string pepper)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));
            if (string.IsNullOrEmpty(pepper))
                throw new InvalidOperationException("OtpPepper chưa cấu hình trong Web.config.");

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(pepper + "|" + code.Trim());
                return Convert.ToBase64String(sha.ComputeHash(bytes));
            }
        }

        public static bool Verify(string code, string storedHash, string pepper)
        {
            if (code == null || storedHash == null)
                return false;
            var h = HashCode(code, pepper);
            return FixedTimeEquals(h, storedHash);
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
