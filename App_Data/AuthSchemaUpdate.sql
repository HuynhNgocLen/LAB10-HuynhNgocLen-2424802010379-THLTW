-- Chạy một lần trên database SachOnline (SQL Server).

IF COL_LENGTH('dbo.KHACHHANG', 'DaXacThucEmail') IS NULL
BEGIN
    ALTER TABLE dbo.KHACHHANG ADD DaXacThucEmail BIT NOT NULL CONSTRAINT DF_KHACHHANG_DaXacThucEmail DEFAULT(0);
END
GO

ALTER TABLE dbo.KHACHHANG ALTER COLUMN MatKhau VARCHAR(256) NOT NULL;
GO

-- Khách hàng đã tồn tại trước khi bật xác thực email: đánh dấu đã xác thực.
UPDATE dbo.KHACHHANG SET DaXacThucEmail = 1;
GO

-- Bảng ADMIN: cột MatKhau (thường varchar(15)) phải đủ dài để lưu hash Argon2id (base64 ~88 ký tự).
ALTER TABLE dbo.ADMIN ALTER COLUMN MatKhau VARCHAR(256) NOT NULL;
GO
