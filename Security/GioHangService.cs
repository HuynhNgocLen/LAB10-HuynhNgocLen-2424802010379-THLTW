using HuynhNgocLen.SachOnline.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace HuynhNgocLen.SachOnline.Services
{
    public static class GioHangService
    {
        private const string SessionKey = "GioHang";

        // Lấy SQL connection string thuần từ EF DbContext (tránh lỗi metadata URI)
        private static string GetConnStr()
        {
            using (var db = new SachOnlineEntities1())
            {
                return db.Database.Connection.ConnectionString;
            }
        }

        // ── Lấy giỏ từ Session ──────────────────────────────────────────
        public static List<GioHang> LayGioHang(HttpSessionStateBase session)
        {
            var gh = session[SessionKey] as List<GioHang>;
            if (gh == null)
            {
                gh = new List<GioHang>();
                session[SessionKey] = gh;
            }
            return gh;
        }

        // ── Khi ĐĂNG NHẬP: tải giỏ từ DB vào Session ───────────────────
        public static void TaiGioHangTuDB(HttpSessionStateBase session, int maKH)
        {
            var gh = new List<GioHang>();

            using (var conn = new SqlConnection(GetConnStr()))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT MaSach, SoLuong FROM GIOHANG_DB WHERE MaKH = @MaKH", conn);
                cmd.Parameters.AddWithValue("@MaKH", maKH);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int maSach = (int)reader["MaSach"];
                        int soLuong = (int)reader["SoLuong"];
                        try
                        {
                            var item = new GioHang(maSach);
                            item.iSoLuong = soLuong;
                            gh.Add(item);
                        }
                        catch { /* bỏ qua nếu sách không còn tồn tại */ }
                    }
                }
            }

            session[SessionKey] = gh;
        }

        // ── Khi ĐĂNG XUẤT: lưu Session → DB rồi xóa Session ────────────
        public static void LuuVaXoaSession(HttpSessionStateBase session, int maKH)
        {
            var gh = session[SessionKey] as List<GioHang>;
            if (gh != null && gh.Count > 0)
                LuuGioHangVaoDB(maKH, gh);
            else
                XoaGioHangTrongDB(maKH);

            session.Remove(SessionKey);
        }

        // ── Thêm / cập nhật 1 sản phẩm ──────────────────────────────────
        public static void ThemSanPham(HttpSessionStateBase session, int maSach, int maKH = 0)
        {
            var gh = LayGioHang(session);
            var item = gh.FirstOrDefault(x => x.iMaSach == maSach);
            if (item == null)
                gh.Add(new GioHang(maSach));
            else
                item.iSoLuong++;

            if (maKH > 0)
                LuuGioHangVaoDB(maKH, gh);
        }

        // ── Cập nhật số lượng ────────────────────────────────────────────
        public static void CapNhat(HttpSessionStateBase session, int maSach, int soLuong, int maKH = 0)
        {
            var gh = LayGioHang(session);
            var item = gh.FirstOrDefault(x => x.iMaSach == maSach);
            if (item != null)
                item.iSoLuong = soLuong < 1 ? 1 : soLuong;

            if (maKH > 0)
                LuuGioHangVaoDB(maKH, gh);
        }

        // ── Xóa 1 sản phẩm ──────────────────────────────────────────────
        public static void XoaSanPham(HttpSessionStateBase session, int maSach, int maKH = 0)
        {
            var gh = LayGioHang(session);
            gh.RemoveAll(x => x.iMaSach == maSach);

            if (maKH > 0)
                LuuGioHangVaoDB(maKH, gh);
        }

        // ── Xóa toàn bộ giỏ ─────────────────────────────────────────────
        public static void XoaToanBo(HttpSessionStateBase session, int maKH = 0)
        {
            session.Remove(SessionKey);
            if (maKH > 0)
                XoaGioHangTrongDB(maKH);
        }

        // ── Ghi toàn bộ giỏ Session → DB ────────────────────────────────
        private static void LuuGioHangVaoDB(int maKH, List<GioHang> gh)
        {
            using (var conn = new SqlConnection(GetConnStr()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        var del = new SqlCommand(
                            "DELETE FROM GIOHANG_DB WHERE MaKH = @MaKH", conn, tran);
                        del.Parameters.AddWithValue("@MaKH", maKH);
                        del.ExecuteNonQuery();

                        foreach (var item in gh)
                        {
                            var ins = new SqlCommand(
                                "INSERT INTO GIOHANG_DB (MaKH, MaSach, SoLuong) VALUES (@MaKH, @MaSach, @SoLuong)",
                                conn, tran);
                            ins.Parameters.AddWithValue("@MaKH", maKH);
                            ins.Parameters.AddWithValue("@MaSach", item.iMaSach);
                            ins.Parameters.AddWithValue("@SoLuong", item.iSoLuong);
                            ins.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void XoaGioHangTrongDB(int maKH)
        {
            using (var conn = new SqlConnection(GetConnStr()))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "DELETE FROM GIOHANG_DB WHERE MaKH = @MaKH", conn);
                cmd.Parameters.AddWithValue("@MaKH", maKH);
                cmd.ExecuteNonQuery();
            }
        }
    }
}