using HuynhNgocLen.SachOnline.Models;
using HuynhNgocLen.SachOnline.Security;
using System;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Controllers
{
    public class UserController : Controller
    {
        private const string SessionOtpPurpose = "OtpPurpose";
        private const string SessionOtpMaKh = "OtpMaKH";
        private const string SessionOtpHash = "OtpHash";
        private const string SessionOtpExpiryUtc = "OtpExpiryUtc";
        private const string SessionOtpSendCount = "OtpSendCount";
        private const string SessionResetPasswordMaKh = "ResetPasswordMaKH";
        private const int OtpValidMinutes = 3;
        private const int OtpMaxSendAttempts = 3;

        private readonly SachOnlineEntities1 db = new SachOnlineEntities1();

        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(FormCollection collection, KHACHHANG kh)
        {
            var sHoTen = (collection["HoTen"] ?? string.Empty).Trim();
            var sTenDN = (collection["TenDN"] ?? string.Empty).Trim();
            var sMatKhau = collection["MatKhau"] ?? string.Empty;
            var sMatKhauNL = collection["MatKhauNL"] ?? string.Empty;
            var sEmail = (collection["Email"] ?? string.Empty).Trim();
            var sDienThoai = (collection["DienThoai"] ?? string.Empty).Trim();
            var sDiaChi = (collection["DiaChi"] ?? string.Empty).Trim();
            var dNgaySinh = collection["NgaySinh"];

            if (string.IsNullOrEmpty(sHoTen)) ViewData["err1"] = "Họ tên không được rỗng";
            else if (string.IsNullOrEmpty(sTenDN)) ViewData["err2"] = "Tên đăng nhập không được rỗng";
            else if (string.IsNullOrEmpty(sMatKhau)) ViewData["err3"] = "Mật khẩu không được rỗng";
            else if (sMatKhau != sMatKhauNL) ViewData["err4"] = "Mật khẩu nhập lại không khớp";
            else if (string.IsNullOrEmpty(sEmail)) ViewData["err5"] = "Email không được rỗng";
            else if (string.IsNullOrEmpty(sDienThoai)) ViewData["err6"] = "Điện thoại không được rỗng";
            else if (string.IsNullOrEmpty(dNgaySinh)) ViewData["err7"] = "Vui lòng chọn ngày sinh";
            else if (sHoTen.Length > 100) ViewData["err1"] = "Họ tên tối đa 100 ký tự.";
            else if (sTenDN.Length > 50) ViewData["err2"] = "Tên đăng nhập tối đa 50 ký tự.";
            else if (sEmail.Length > 100) ViewData["err5"] = "Email tối đa 100 ký tự.";
            else if (sDienThoai.Length > 20) ViewData["err6"] = "Số điện thoại tối đa 20 ký tự (chỉ nhập số, không cách).";
            else if (sDiaChi.Length > 200) ViewData["errDiaChi"] = "Địa chỉ tối đa 200 ký tự.";
            else if (db.KHACHHANGs.SingleOrDefault(n => n.TaiKhoan == sTenDN) != null) ViewBag.ThongBao = "Tên đăng nhập đã tồn tại";
            else if (db.KHACHHANGs.SingleOrDefault(n => n.Email == sEmail) != null) ViewBag.ThongBao = "Email đã tồn tại";
            else
            {
                kh.HoTen = sHoTen;
                kh.TaiKhoan = sTenDN;
                kh.MatKhau = Argon2PasswordHasher.HashPassword(sMatKhau);
                kh.Email = sEmail;
                kh.DiaChi = string.IsNullOrEmpty(sDiaChi) ? null : sDiaChi;
                kh.DienThoai = sDienThoai;
                kh.NgaySinh = DateTime.Parse(dNgaySinh);
                kh.DaXacThucEmail = false;

                db.KHACHHANGs.Add(kh);
                db.SaveChanges();

                if (!TryBeginOtpFlow("register", kh.MaKH, kh.Email))
                    return View();

                return RedirectToAction(nameof(XacThucMa));
            }
            return View();
        }

        [HttpGet]
        public ActionResult DangNhap(string url)
        {
            ViewBag.Url = url;
            if (Request.QueryString["hetHan"] == "1")
                ViewBag.ThongBao = "Phiên đăng nhập đã hết hạn (3 giờ). Vui lòng đăng nhập lại.";
            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(FormCollection f, string url)
        {
            var sTenDN = (f["TenDN"] ?? string.Empty).Trim();
            var sMatKhau = f["MatKhau"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sTenDN))
                ViewData["Err1"] = "Vui lòng nhập tên đăng nhập";
            if (string.IsNullOrWhiteSpace(sMatKhau))
                ViewData["Err2"] = "Vui lòng nhập mật khẩu";

            if (ViewData["Err1"] != null || ViewData["Err2"] != null)
            {
                ViewBag.Url = url;
                return View();
            }

            var kh = db.KHACHHANGs.SingleOrDefault(n => n.TaiKhoan == sTenDN);
            if (kh == null || !Argon2PasswordHasher.Verify(sMatKhau, kh.MatKhau))
            {
                ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng";
                ViewBag.Url = url;
                return View();
            }

            if (!Argon2PasswordHasher.LooksLikeArgon2Hash(kh.MatKhau))
            {
                kh.MatKhau = Argon2PasswordHasher.HashPassword(sMatKhau);
                db.SaveChanges();
            }

            IssueAuthCookieAndSession(kh);
            return RedirectToAction("Index", "SachOnline");
        }

        [HttpGet]
        public ActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuenMatKhau(FormCollection f)
        {
            var raw = (f["TenDNHoacEmail"] ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(raw))
            {
                ViewData["Err1"] = "Vui lòng nhập tên đăng nhập hoặc email.";
                return View();
            }

            var kh = db.KHACHHANGs.SingleOrDefault(k => k.TaiKhoan == raw)
                     ?? db.KHACHHANGs.SingleOrDefault(k => k.Email == raw);

            if (kh == null)
            {
                ViewBag.ThongBao = "Không tìm thấy tài khoản trùng với thông tin đã nhập.";
                return View();
            }

            if (!kh.DaXacThucEmail)
            {
                ViewBag.ThongBao = "Tài khoản chưa xác thực email. Vui lòng dùng liên kết \"Gửi lại mã xác thực\" trên trang đăng nhập để kích hoạt trước.";
                return View();
            }

            if (!TryBeginOtpFlow("reset_password", kh.MaKH, kh.Email))
                return View();

            TempData["Info"] = "Đã gửi mã tới email của bạn.";
            return RedirectToAction(nameof(XacThucMa));
        }

        [HttpGet]
        public ActionResult DatLaiMatKhau()
        {
            if (Session[SessionResetPasswordMaKh] == null)
            {
                TempData["Info"] = "Phiên đặt lại mật khẩu không hợp lệ. Hãy gửi mã lại từ trang Quên mật khẩu.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatLaiMatKhau(FormCollection f)
        {
            var maKhObj = Session[SessionResetPasswordMaKh];
            if (maKhObj == null)
            {
                TempData["Info"] = "Phiên hết hạn. Vui lòng thử lại từ trang Quên mật khẩu.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            var maKh = (int)maKhObj;
            var kh = db.KHACHHANGs.SingleOrDefault(k => k.MaKH == maKh);
            if (kh == null)
            {
                Session.Remove(SessionResetPasswordMaKh);
                return RedirectToAction(nameof(QuenMatKhau));
            }

            var m1 = f["MatKhau"] ?? string.Empty;
            var m2 = f["MatKhauNL"] ?? string.Empty;
            if (string.IsNullOrEmpty(m1))
                ViewData["Err1"] = "Vui lòng nhập mật khẩu mới.";
            else if (m1.Length < 6)
                ViewData["Err1"] = "Mật khẩu tối thiểu 6 ký tự.";
            else if (m1 != m2)
                ViewData["Err2"] = "Mật khẩu nhập lại không khớp.";

            if (ViewData["Err1"] != null || ViewData["Err2"] != null)
                return View();

            kh.MatKhau = Argon2PasswordHasher.HashPassword(m1);
            db.SaveChanges();
            Session.Remove(SessionResetPasswordMaKh);
            TempData["Info"] = "Đã đặt lại mật khẩu. Bạn có thể đăng nhập.";
            return RedirectToAction(nameof(DangNhap));
        }

        [HttpGet]
        public ActionResult GuiLaiXacThucEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiLaiXacThucEmail(FormCollection f)
        {
            var raw = (f["TenDNHoacEmail"] ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(raw))
            {
                ViewData["Err1"] = "Vui lòng nhập tên đăng nhập hoặc email.";
                return View();
            }

            var kh = db.KHACHHANGs.SingleOrDefault(k => k.TaiKhoan == raw)
                     ?? db.KHACHHANGs.SingleOrDefault(k => k.Email == raw);

            if (kh == null)
            {
                ViewBag.ThongBao = "Không tìm thấy tài khoản trùng với thông tin đã nhập.";
                return View();
            }

            if (kh.DaXacThucEmail)
            {
                ViewBag.ThongBao = "Tài khoản này đã xác thực email. Bạn có thể đăng nhập bình thường.";
                return View();
            }

            if (!TryBeginOtpFlow("register", kh.MaKH, kh.Email))
                return View();

            TempData["Info"] = "Đã gửi mã xác thực tới email đăng ký của bạn.";
            return RedirectToAction(nameof(XacThucMa));
        }

        [HttpGet]
        public ActionResult XacThucMa()
        {
            if (Session[SessionOtpPurpose] == null || Session[SessionOtpMaKh] == null)
            {
                ViewBag.ThongBao = "Phiên xác thực không hợp lệ. Vui lòng đăng nhập hoặc đăng ký lại.";
                return View();
            }

            ViewBag.OtpPurpose = Session[SessionOtpPurpose] as string;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacThucMa(FormCollection f)
        {
            var code = (f["MaXacThuc"] ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code) || code.Length != OtpService.CodeLength)
            {
                ViewBag.ThongBao = "Vui lòng nhập đúng " + OtpService.CodeLength + " chữ số.";
                ViewBag.OtpPurpose = Session[SessionOtpPurpose] as string;
                return View();
            }

            var purpose = Session[SessionOtpPurpose] as string;
            var maKhObj = Session[SessionOtpMaKh];
            var hash = Session[SessionOtpHash] as string;
            var expiryObj = Session[SessionOtpExpiryUtc];

            if (purpose == null || maKhObj == null || hash == null || expiryObj == null)
            {
                ViewBag.ThongBao = "Phiên hết hạn. Vui lòng thử lại.";
                return View();
            }

            var expiry = (DateTime)expiryObj;
            if (DateTime.UtcNow > expiry)
            {
                ClearOtpSession();
                ViewBag.ThongBao = "Mã đã hết hạn. Vui lòng gửi lại mã hoặc thử lại.";
                return View();
            }

            var pepper = ConfigurationManager.AppSettings["OtpPepper"];
            if (!OtpService.Verify(code, hash, pepper))
            {
                ViewBag.ThongBao = "Mã xác thực không đúng.";
                ViewBag.OtpPurpose = purpose;
                return View();
            }

            var maKh = (int)maKhObj;
            var kh = db.KHACHHANGs.SingleOrDefault(k => k.MaKH == maKh);
            if (kh == null)
            {
                ClearOtpSession();
                ViewBag.ThongBao = "Không tìm thấy tài khoản.";
                return View();
            }

            if (purpose == "reset_password")
            {
                ClearOtpSession();
                Session[SessionResetPasswordMaKh] = maKh;
                TempData["Info"] = "Xác thực thành công. Đặt mật khẩu mới bên dưới.";
                return RedirectToAction(nameof(DatLaiMatKhau));
            }

            ClearOtpSession();

            if (purpose == "register")
            {
                kh.DaXacThucEmail = true;
                db.SaveChanges();
                TempData["Info"] = "Xác thực email thành công. Bạn có thể đăng nhập.";
                return RedirectToAction(nameof(DangNhap));
            }

            ViewBag.ThongBao = "Luồng xác thực không hợp lệ.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiLaiMa()
        {
            var purpose = Session[SessionOtpPurpose] as string;
            var maKhObj = Session[SessionOtpMaKh];
            if (purpose == null || maKhObj == null)
                return RedirectToAction(nameof(DangNhap));

            var maKh = (int)maKhObj;
            var kh = db.KHACHHANGs.SingleOrDefault(k => k.MaKH == maKh);
            if (kh == null)
                return RedirectToAction(nameof(DangNhap));

            if (!TryBeginOtpFlow(purpose, maKh, kh.Email))
                return RedirectToAction(nameof(XacThucMa));

            TempData["Info"] = "Đã gửi lại mã tới email của bạn.";
            return RedirectToAction(nameof(XacThucMa));
        }

        public ActionResult DangXuat()
        {
            Session["TaiKhoan"] = null;
            var c = new HttpCookie(JwtTokenService.CookieName, "")
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = Request.IsSecureConnection
            };
            Response.Cookies.Add(c);

            return RedirectToAction("Index", "SachOnline");
        }

        private void IssueAuthCookieAndSession(KHACHHANG kh)
        {
            Session["TaiKhoan"] = kh;

            var token = JwtTokenService.CreateToken(kh.MaKH, kh.TaiKhoan, kh.Email);
            var minutes = int.TryParse(ConfigurationManager.AppSettings["JwtExpiryMinutes"], out var m) ? m : 180;
            var cookie = new HttpCookie(JwtTokenService.CookieName, token)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(minutes)
            };
            Response.Cookies.Add(cookie);
        }

        private bool TryBeginOtpFlow(string purpose, int maKh, string email)
        {
            var pepper = ConfigurationManager.AppSettings["OtpPepper"];
            if (string.IsNullOrWhiteSpace(pepper) || pepper.Length < 8)
            {
                var msg = "OtpPepper trong Web.config chưa được cấu hình (tối thiểu 8 ký tự).";
                ViewBag.ThongBao = msg;
                TempData["ThongBao"] = msg;
                return false;
            }

            var sendCount = Session[SessionOtpSendCount] as int? ?? 0;
            if (sendCount >= OtpMaxSendAttempts)
            {
                var msg = "Bạn đã yêu cầu gửi mã quá " + OtpMaxSendAttempts + " lần. Vui lòng thử lại sau.";
                ViewBag.ThongBao = msg;
                TempData["ThongBao"] = msg;
                return false;
            }

            var code = OtpService.GenerateNumericCode();
            Session[SessionOtpPurpose] = purpose;
            Session[SessionOtpMaKh] = maKh;
            Session[SessionOtpHash] = OtpService.HashCode(code, pepper);
            Session[SessionOtpExpiryUtc] = DateTime.UtcNow.AddMinutes(OtpValidMinutes);
            Session[SessionOtpSendCount] = sendCount + 1;

            var len = OtpService.CodeLength;
            string subject;
            string bodyHtml;
            switch (purpose)
            {
                case "register":
                    subject = "Xác thực đăng ký — SachOnline";
                    bodyHtml = EmailTemplates.RegistrationOtpHtml(code, len, OtpValidMinutes);
                    break;
                case "reset_password":
                    subject = "Đặt lại mật khẩu — SachOnline";
                    bodyHtml = EmailTemplates.ResetPasswordOtpHtml(code, len, OtpValidMinutes);
                    break;
                default:
                    var badPurpose = "Luồng xác thực không hợp lệ.";
                    ViewBag.ThongBao = badPurpose;
                    TempData["ThongBao"] = badPurpose;
                    return false;
            }

            if (EmailService.TrySend(email, subject, bodyHtml, true, out var err))
                return true;

#if DEBUG
            TempData["DevOtp"] = code;
            TempData["MailWarning"] = err ?? "Không gửi được email.";
            return true;
#else
            var mailErr = "Không gửi được email xác thực: " + err + ". Kiểm tra cấu hình SMTP trong Web.config.";
            ViewBag.ThongBao = mailErr;
            TempData["ThongBao"] = mailErr;
            ClearOtpSession();
            return false;
#endif
        }

        private void ClearOtpSession()
        {
            Session.Remove(SessionOtpPurpose);
            Session.Remove(SessionOtpMaKh);
            Session.Remove(SessionOtpHash);
            Session.Remove(SessionOtpExpiryUtc);
            Session.Remove(SessionOtpSendCount);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
