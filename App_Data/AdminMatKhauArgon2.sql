-- Chạy trên database SachOnline khi gặp lỗi "String or binary data would be truncated" ở ADMIN.MatKhau.

ALTER TABLE dbo.ADMIN ALTER COLUMN MatKhau VARCHAR(256) NOT NULL;
GO

-- Tùy chọn: đặt lại mật khẩu plaintext (lần đăng nhập sau sẽ lưu hash Argon2).
-- UPDATE dbo.ADMIN SET MatKhau = 'arry1id';
-- GO
