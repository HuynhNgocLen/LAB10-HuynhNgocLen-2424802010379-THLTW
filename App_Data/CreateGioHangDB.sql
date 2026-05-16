-- =============================================
-- Tạo bảng GIOHANG_DB để lưu giỏ hàng vào database
-- Giỏ hàng sẽ được lưu khi đăng xuất và tải lại khi đăng nhập
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GIOHANG_DB' AND xtype='U')
BEGIN
    CREATE TABLE GIOHANG_DB (
        MaKH     INT NOT NULL,
        MaSach   INT NOT NULL,
        SoLuong  INT NOT NULL DEFAULT 1,
        CONSTRAINT PK_GIOHANG_DB PRIMARY KEY (MaKH, MaSach),
        CONSTRAINT FK_GIOHANG_DB_KH FOREIGN KEY (MaKH) REFERENCES KHACHHANG(MaKH),
        CONSTRAINT FK_GIOHANG_DB_SACH FOREIGN KEY (MaSach) REFERENCES SACH(MaSach)
    );
    PRINT N'Đã tạo bảng GIOHANG_DB thành công.';
END
ELSE
BEGIN
    PRINT N'Bảng GIOHANG_DB đã tồn tại.';
END
GO
