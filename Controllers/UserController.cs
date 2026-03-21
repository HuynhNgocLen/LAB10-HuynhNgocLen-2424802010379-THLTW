using HuynhNgocLen.SachOnline.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Controllers
{
    public class UserController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(FormCollection collection, KHACHHANG kh)
        {
            var sHoTen = collection["HoTen"];
            var sTenDN = collection["TenDN"];
            var sMatKhau = collection["MatKhau"];
            var sMatKhauNL = collection["MatKhauNL"];
            var sEmail = collection["Email"];
            var sDienThoai = collection["DienThoai"];
            var sDiaChi = collection["DiaChi"];
            var dNgaySinh = collection["NgaySinh"];

            // Kiểm tra các trường dữ liệu
            if (String.IsNullOrEmpty(sHoTen)) ViewData["err1"] = "Họ tên không được rỗng";
            else if (String.IsNullOrEmpty(sTenDN)) ViewData["err2"] = "Tên đăng nhập không được rỗng";
            else if (String.IsNullOrEmpty(sMatKhau)) ViewData["err3"] = "Mật khẩu không được rỗng";
            else if (sMatKhau != sMatKhauNL) ViewData["err4"] = "Mật khẩu nhập lại không khớp";
            else if (String.IsNullOrEmpty(sEmail)) ViewData["err5"] = "Email không được rỗng";
            else if (String.IsNullOrEmpty(sDienThoai)) ViewData["err6"] = "Điện thoại không được rỗng";
            else if (String.IsNullOrEmpty(dNgaySinh)) ViewData["err7"] = "Vui lòng chọn ngày sinh";
            else if (db.KHACHHANGs.SingleOrDefault(n => n.TaiKhoan == sTenDN) != null) ViewBag.ThongBao = "Tên đăng nhập đã tồn tại";
            else if (db.KHACHHANGs.SingleOrDefault(n => n.Email == sEmail) != null) ViewBag.ThongBao = "Email đã tồn tại";
            else
            {
                kh.HoTen = sHoTen;
                kh.TaiKhoan = sTenDN;
                kh.MatKhau = sMatKhau;
                kh.Email = sEmail;
                kh.DiaChi = sDiaChi;
                kh.DienThoai = sDienThoai;
                kh.NgaySinh = DateTime.Parse(dNgaySinh);

                db.KHACHHANGs.Add(kh);
                db.SaveChanges();

                return RedirectToAction("DangNhap");
            }
            return View();
        }

        //Dang Nhap
        [HttpGet]
        public ActionResult DangNhap(string url)
        {
            ViewBag.Url = url;
            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(FormCollection f, string url)
        {
            var sTenDN = f["TenDN"];
            var sMatKhau = f["MatKhau"];

            KHACHHANG kh = db.KHACHHANGs.SingleOrDefault(
                n => n.TaiKhoan == sTenDN && n.MatKhau == sMatKhau);

            if (kh != null)
            {
                Session["TaiKhoan"] = kh;

                if (!string.IsNullOrEmpty(url))
                    return Redirect(url);
                else
                    return RedirectToAction("Index", "SachOnline");
            }
            else
            {
                ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng";
            }

            return View();
        }

        // Đăng xuất
        public ActionResult DangXuat()
        {
            Session["TaiKhoan"] = null;
            return RedirectToAction("Index", "SachOnline");
        }

    }
}