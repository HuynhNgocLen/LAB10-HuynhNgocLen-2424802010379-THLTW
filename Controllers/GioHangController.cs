using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HuynhNgocLen.SachOnline.Models;
using HuynhNgocLen.SachOnline.Services;
using Newtonsoft.Json.Linq;

namespace HuynhNgocLen.SachOnline.Controllers
{
    public class GioHangController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        /// <summary>
        /// Tạo mã UUID version 7 (sắp xếp theo thời gian).
        /// </summary>
        private static string NewUuidV7()
        {
            long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            byte[] timestampBytes = BitConverter.GetBytes(ms);
            if (BitConverter.IsLittleEndian) Array.Reverse(timestampBytes);
            byte[] randomBytes = new byte[10];
            new Random().NextBytes(randomBytes);
            byte[] guidBytes = new byte[16];
            Array.Copy(timestampBytes, 2, guidBytes, 0, 6);
            Array.Copy(randomBytes, 0, guidBytes, 6, 10);
            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x70);
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
            return new Guid(guidBytes).ToString();
        }

        /// <summary>
        /// Lấy MaKH của khách hàng đang đăng nhập (0 nếu chưa đăng nhập).
        /// </summary>
        private int LayMaKH()
        {
            var kh = Session["TaiKhoan"] as KHACHHANG;
            return kh != null ? kh.MaKH : 0;
        }

        public List<GioHang> LayGioHang()
        {
            return GioHangService.LayGioHang(Session);
        }

        public ActionResult ThemGioHang(int ms, string url)
        {
            int maKH = LayMaKH();

            // Lấy tên sách trước khi thêm để hiển thị toast
            List<GioHang> lstCart = LayGioHang();
            GioHang existing = lstCart.Find(n => n.iMaSach == ms);

            // Thêm/cập nhật qua service (tự đồng bộ DB nếu đã đăng nhập)
            GioHangService.ThemSanPham(Session, ms, maKH);

            // Lấy lại danh sách để lấy tên sách
            lstCart = LayGioHang();
            GioHang sp = lstCart.Find(n => n.iMaSach == ms);

            if (existing == null)
            {
                TempData["CartToast"] = "Đã thêm \"" + (sp != null ? sp.sTenSach : "") + "\" vào giỏ hàng.";
            }
            else
            {
                TempData["CartToast"] = "Đã cập nhật số lượng \"" + (sp != null ? sp.sTenSach : "") + "\" trong giỏ hàng.";
            }

            if (string.IsNullOrEmpty(url))
            {
                return RedirectToAction("Index", "SachOnline");
            }
            return Redirect(url);
        }

        private int TongSoLuong()
        {
            int iTongSoLuong = 0;
            List<GioHang> lstCart = Session["GioHang"] as List<GioHang>;
            if (lstCart != null)
            {
                iTongSoLuong = lstCart.Sum(n => n.iSoLuong);
            }
            return iTongSoLuong;
        }

        private double TongTien()
        {
            double dTongTien = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                dTongTien = lstGioHang.Sum(n => n.dThanhTien);
            }
            return dTongTien;
        }

        public ActionResult GioHang()
        {
            List<GioHang> lstGioHang = LayGioHang();
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            ViewBag.OmitStoreSidebar = true;
            return View(lstGioHang);
        }

        public ActionResult GioHangPartial()
        {
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            return PartialView();
        }

        public ActionResult XoaSPKhoiGioHang(int iMaSach)
        {
            int maKH = LayMaKH();
            // Xóa qua service (tự đồng bộ DB nếu đã đăng nhập)
            GioHangService.XoaSanPham(Session, iMaSach, maKH);
            return RedirectToAction("GioHang");
        }

        public ActionResult CapNhatGioHang(int iMaSach, FormCollection f)
        {
            int maKH = LayMaKH();
            int soLuong = int.Parse(f["txtSoLuong"].ToString());
            // Cập nhật qua service (tự đồng bộ DB nếu đã đăng nhập)
            GioHangService.CapNhat(Session, iMaSach, soLuong, maKH);
            return RedirectToAction("GioHang");
        }

        public ActionResult XoaGioHang()
        {
            int maKH = LayMaKH();
            // Xóa toàn bộ qua service (tự đồng bộ DB nếu đã đăng nhập)
            GioHangService.XoaToanBo(Session, maKH);
            return RedirectToAction("GioHang");
        }

        [HttpGet]
        public ActionResult DatHang()
        {
            if (Session["TaiKhoan"] == null || Session["TaiKhoan"].ToString() == "")
            {
                return RedirectToAction("DangNhap", "User",
                    new { url = Request.Url.ToString() });
            }

            var khSession = (KHACHHANG)Session["TaiKhoan"];
            var khDb = db.KHACHHANGs.Find(khSession.MaKH);
            if (khDb == null)
            {
                Session["TaiKhoan"] = null;
                return RedirectToAction("DangNhap", "User");
            }

            if (!khDb.DaXacThucEmail)
            {
                TempData["Info"] = "Đặt hàng cần email đã xác thực. Vui lòng hoàn tất xác thực email (mã gửi về hộp thư đăng ký).";
                return RedirectToAction("GuiLaiXacThucEmail", "User");
            }

            List<GioHang> lstGioHang = LayGioHang();
            if (lstGioHang.Count == 0)
            {
                return RedirectToAction("Index", "SachOnline");
            }

            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            ViewBag.OmitStoreSidebar = true;

            return View(lstGioHang);
        }

        [HttpPost]
        public ActionResult DatHang(FormCollection f)
        {
            List<GioHang> lstCart = LayGioHang();
            KHACHHANG kh = (KHACHHANG)Session["TaiKhoan"];

            if (kh == null)
            {
                return RedirectToAction("DangNhap", "User");
            }

            var khDb = db.KHACHHANGs.Find(kh.MaKH);
            if (khDb == null)
            {
                Session["TaiKhoan"] = null;
                return RedirectToAction("DangNhap", "User");
            }

            if (!khDb.DaXacThucEmail)
            {
                TempData["Info"] = "Đặt hàng cần email đã xác thực. Vui lòng hoàn tất xác thực email.";
                return RedirectToAction("GuiLaiXacThucEmail", "User");
            }

            // Lấy phương thức thanh toán
            var phuongThuc = f["PhuongThucThanhToan"] ?? "COD";

            // ── Tạo đơn hàng ──────────────────────────────────
            DONDATHANG ddh = new DONDATHANG();
            ddh.MaKH = khDb.MaKH;
            ddh.NgayDat = DateTime.Now;

            var ngayGiao = f["NgayGiao"];
            if (!string.IsNullOrEmpty(ngayGiao))
            {
                ddh.NgayGiao = DateTime.Parse(ngayGiao);
            }

            // Lưu phương thức thanh toán chính thức
            string pttt = f["PhuongThucThanhToan"];
            if (pttt == "MOMO") ddh.PhuongThucThanhToan = "Ví MoMo";
            else if (pttt == "BANK") ddh.PhuongThucThanhToan = "Chuyển khoản";
            else ddh.PhuongThucThanhToan = "Tiền mặt";

            // Tạo mã tra cứu UUID v7
            ddh.MaTraCuu = NewUuidV7();

            // 0: Mới đặt (COD/MoMo chưa xong)
            // 1: Chờ xác thực thanh toán (BANK transfer)
            ddh.TinhTrangGiaoHang = (phuongThuc == "BANK") ? 1 : 0;
            ddh.DaThanhToan = false;

            db.DONDATHANGs.Add(ddh);
            db.SaveChanges();

            foreach (var item in lstCart)
            {
                CHITIETDATHANG ctdh = new CHITIETDATHANG();
                ctdh.MaDonHang = ddh.MaDonHang;
                ctdh.MaSach = item.iMaSach;
                ctdh.SoLuong = item.iSoLuong;
                ctdh.DonGia = (decimal)item.dDonGia;
                db.CHITIETDATHANGs.Add(ctdh);
            }
            db.SaveChanges();

            // Xóa giỏ hàng
            GioHangService.XoaToanBo(Session, kh.MaKH);

            // ── Nếu chọn MoMo → redirect sang cổng thanh toán MoMo ──
            if (phuongThuc == "MOMO")
            {
                long amount = (long)lstCart.Sum(x => x.dThanhTien);
                // MoMo yêu cầu amount >= 1000 và <= 50.000.000
                if (amount < 1000) amount = 1000;

                string orderId = "SACH" + ddh.MaDonHang + "_" + DateTime.Now.Ticks;
                string orderInfo = "Thanh toán đơn hàng #" + ddh.MaDonHang + " - SachOnline";

                // URL MoMo sẽ redirect về sau khi thanh toán
                string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority;
                string redirectUrl = baseUrl + Url.Action("MoMoReturn", "GioHang");
                string ipnUrl = baseUrl + Url.Action("MoMoIPN", "GioHang");

                // Lưu MaDonHang vào extraData (base64) để lấy lại khi callback
                string extraData = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        "{\"maDonHang\":" + ddh.MaDonHang + "}"
                    ));

                var momoResult = MoMoService.CreatePayment(
                    orderId, amount, orderInfo, redirectUrl, ipnUrl, extraData);

                if (momoResult.Success)
                {
                    // Redirect user sang trang thanh toán MoMo
                    return Redirect(momoResult.PayUrl);
                }
                else
                {
                    // Lỗi MoMo → vẫn tạo đơn thành công nhưng báo lỗi thanh toán
                    TempData["PhuongThucTT"] = "MOMO_ERROR";
                    TempData["MaDonHang"] = ddh.MaDonHang;
                    TempData["MoMoError"] = momoResult.Message;
                    return RedirectToAction("XacNhanDonHang", "GioHang");
                }
            }

            // ── COD / Khác → chuyển trang xác nhận ──
            TempData["PhuongThucTT"] = phuongThuc;
            TempData["MaDonHang"] = ddh.MaDonHang;
            TempData["MaTraCuu"] = ddh.MaTraCuu;
            TempData["TongTien"] = lstCart.Sum(x => x.dThanhTien);
            return RedirectToAction("XacNhanDonHang", "GioHang");
        }

        // ── MoMo redirect user về sau khi thanh toán ─────────────────
        [HttpGet]
        public ActionResult MoMoReturn()
        {
            // Lấy tham số MoMo trả về qua query string
            var resultCode = Request.QueryString["resultCode"];
            var orderId = Request.QueryString["orderId"];
            var message = Request.QueryString["message"];
            var extraData = Request.QueryString["extraData"] ?? "";
            var transId = Request.QueryString["transId"];

            int maDonHang = 0;
            // Parse extraData để lấy maDonHang
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(extraData));
                var obj = JObject.Parse(json);
                maDonHang = obj["maDonHang"]?.Value<int>() ?? 0;
            }
            catch { }

            ViewBag.OmitStoreSidebar = true;

            if (resultCode == "0")
            {
                // Thanh toán thành công
                // Cập nhật trạng thái đơn hàng (phòng trường hợp IPN chưa kịp đến)
                if (maDonHang > 0)
                {
                    var ddh = db.DONDATHANGs.Find(maDonHang);
                    if (ddh != null && ddh.DaThanhToan != true)
                    {
                        ddh.DaThanhToan = true;
                        db.SaveChanges();
                    }
                }

                TempData["PhuongThucTT"] = "MOMO";
                TempData["MaDonHang"] = maDonHang;
                TempData["MoMoTransId"] = transId;
                return RedirectToAction("XacNhanDonHang");
            }
            else
            {
                // Thanh toán thất bại / hủy
                TempData["PhuongThucTT"] = "MOMO_FAIL";
                TempData["MaDonHang"] = maDonHang;
                TempData["MoMoError"] = message ?? "Thanh toán MoMo không thành công.";
                return RedirectToAction("XacNhanDonHang");
            }
        }

        // ── MoMo IPN (Instant Payment Notification) ──────────────────
        // MoMo gọi POST đến URL này khi thanh toán hoàn tất
        [HttpPost]
        public ActionResult MoMoIPN()
        {
            try
            {
                string body;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    body = reader.ReadToEnd();
                }

                var json = JObject.Parse(body);
                int resultCode = json["resultCode"]?.Value<int>() ?? -1;
                string extraData = json["extraData"]?.Value<string>() ?? "";
                string signature = json["signature"]?.Value<string>() ?? "";

                // Verify signature
                bool verified = MoMoService.VerifySignature(
                    json["partnerCode"]?.Value<string>() ?? "",
                    json["orderId"]?.Value<string>() ?? "",
                    json["requestId"]?.Value<string>() ?? "",
                    json["amount"]?.Value<long>() ?? 0,
                    json["orderInfo"]?.Value<string>() ?? "",
                    json["orderType"]?.Value<string>() ?? "",
                    json["transId"]?.Value<long>() ?? 0,
                    resultCode,
                    json["message"]?.Value<string>() ?? "",
                    json["payType"]?.Value<string>() ?? "",
                    json["responseTime"]?.Value<long>() ?? 0,
                    extraData,
                    signature
                );

                if (verified && resultCode == 0)
                {
                    // Parse maDonHang từ extraData
                    int maDonHang = 0;
                    try
                    {
                        var extraJson = System.Text.Encoding.UTF8.GetString(
                            Convert.FromBase64String(extraData));
                        var extraObj = JObject.Parse(extraJson);
                        maDonHang = extraObj["maDonHang"]?.Value<int>() ?? 0;
                    }
                    catch { }

                    if (maDonHang > 0)
                    {
                        using (var dbIpn = new SachOnlineEntities1())
                        {
                            var ddh = dbIpn.DONDATHANGs.Find(maDonHang);
                            if (ddh != null)
                            {
                                ddh.DaThanhToan = true;
                                dbIpn.SaveChanges();
                            }
                        }
                    }
                }

                // MoMo yêu cầu trả về HTTP 204 No Content
                return new HttpStatusCodeResult(204);
            }
            catch
            {
                return new HttpStatusCodeResult(204);
            }
        }

        public ActionResult XacNhanDonHang()
        {
            ViewBag.OmitStoreSidebar = true;
            return View();
        }
        [HttpPost]
        public ActionResult HuyDonHang(int id)
        {
            if (Session["TaiKhoan"] == null) return RedirectToAction("DangNhap", "User");
            var kh = (KHACHHANG)Session["TaiKhoan"];
            var ddh = db.DONDATHANGs.SingleOrDefault(d => d.MaDonHang == id && d.MaKH == kh.MaKH);

            if (ddh != null && ddh.TinhTrangGiaoHang == 0)
            {
                ddh.TinhTrangGiaoHang = 4; // Hủy
                db.SaveChanges();
                TempData["UserMsg"] = "Đã hủy đơn hàng #" + id + " thành công.";
            }
            return RedirectToAction("DonHang", "User");
        }
    }
}