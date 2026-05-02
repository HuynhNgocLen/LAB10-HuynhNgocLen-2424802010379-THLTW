using Konscious.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;

namespace HuynhNgocLen.SachOnline.Security
{
    /// <summary>Băm và kiểm tra mật khẩu bằng Argon2id.</summary>
    public static class Argon2PasswordHasher
    {
        private const byte FormatVersion = 1;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            var hash = HashCore(password, salt);
            var payload = new byte[1 + SaltSize + HashSize];
            payload[0] = FormatVersion;
            Buffer.BlockCopy(salt, 0, payload, 1, SaltSize);
            Buffer.BlockCopy(hash, 0, payload, 1 + SaltSize, HashSize);
            return Convert.ToBase64String(payload);
        }

        private static byte[] HashCore(string password, byte[] salt)
        {
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 2;
                argon2.MemorySize = 65536;
                argon2.Iterations = 4;
                return argon2.GetBytes(HashSize);
            }
        }

        /// <summary>
        /// Trả về true nếu khớp hash Argon2id hoặc mật khẩu lưu dạng plaintext (dữ liệu cũ).
        /// </summary>
        public static bool Verify(string password, string storedHash)
        {
            if (password == null || storedHash == null)
                return false;

            if (TryVerifyArgon(storedHash, password, out bool argonOk))
                return argonOk;

            return string.Equals(password, storedHash, StringComparison.Ordinal);
        }

        public static bool LooksLikeArgon2Hash(string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;
            if (!TryParsePayload(storedHash, out var payload))
                return false;
            return payload.Length == 1 + SaltSize + HashSize && payload[0] == FormatVersion;
        }

        private static bool TryVerifyArgon(string storedHash, string password, out bool ok)
        {
            ok = false;
            if (!TryParsePayload(storedHash, out var payload))
                return false;
            if (payload.Length != 1 + SaltSize + HashSize || payload[0] != FormatVersion)
                return false;

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(payload, 1, salt, 0, SaltSize);
            var expected = new byte[HashSize];
            Buffer.BlockCopy(payload, 1 + SaltSize, expected, 0, HashSize);
            var actual = HashCore(password, salt);
            ok = FixedTimeEquals(actual, expected);
            return true;
        }

        private static bool TryParsePayload(string storedHash, out byte[] payload)
        {
            payload = null;
            try
            {
                payload = Convert.FromBase64String(storedHash);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            var d = 0;
            for (var i = 0; i < a.Length; i++)
                d |= a[i] ^ b[i];
            return d == 0;
        }
    }
}
