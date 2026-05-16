using System;

namespace HuynhNgocLen.SachOnline.Services
{
    public static class UuidHelper
    {
        /// <summary>
        /// Tạo mã UUID version 7 (sắp xếp theo thời gian).
        /// Cấu trúc: [48 bits timestamp] [4 bits version] [12 bits random] [2 bits variant] [62 bits random]
        /// </summary>
        public static string NewUuidV7()
        {
            // 1. Lấy timestamp hiện tại (milis giây)
            long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            byte[] timestampBytes = BitConverter.GetBytes(ms);
            
            // C# BitConverter trả về Little Endian trên hầu hết máy Windows, cần đảo ngược sang Big Endian cho UUID
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes);
            }

            // 2. Tạo mảng byte ngẫu nhiên cho phần còn lại
            byte[] randomBytes = new byte[10];
            new Random().NextBytes(randomBytes);

            // 3. Kết hợp vào mảng 16 bytes (128 bits)
            byte[] guidBytes = new byte[16];
            // 6 bytes đầu là timestamp (48 bits) - timestampBytes có 8 bytes, lấy 6 bytes cuối (khi đã đảo ngược)
            Array.Copy(timestampBytes, 2, guidBytes, 0, 6);
            // 10 bytes sau là random
            Array.Copy(randomBytes, 0, guidBytes, 6, 10);

            // 4. Thiết lập Version (7) và Variant (1)
            // Version 7: bit 48-51 là 0111 (0x70)
            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x70);
            // Variant 1: bit 64-65 là 10 (0x80)
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

            return new Guid(guidBytes).ToString();
        }
    }
}
