using HuynhNgocLen.SachOnline.Models;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class HomeController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        public ActionResult Dashboard()
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            ViewBag.TongDonHang = db.DONDATHANGs.Count();
            ViewBag.TongDoanhThu = db.CHITIETDATHANGs.Sum(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0;
            ViewBag.SoKhachHang = db.KHACHHANGs.Count();
            ViewBag.SoSach = db.SACHes.Count();

            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(FormCollection f)
        {
            var sTenDN = f["UserName"];
            var sMatKhau = f["Password"];

            if (string.IsNullOrEmpty(sTenDN))
            {
                ViewBag.ThongBao = "Vui lòng nhập tên đăng nhập!";
            }
            else if (string.IsNullOrEmpty(sMatKhau))
            {
                ViewBag.ThongBao = "Vui lòng nhập mật khẩu!";
            }
            else
            {
                var ad = db.ADMIN.SingleOrDefault(n => n.TenDN == sTenDN && n.MatKhau == sMatKhau);
                if (ad != null)
                {
                    Session["Admin"] = ad;
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng!";
                }
            }
            return View();
        }

        public ActionResult Logout()
        {
            Session["Admin"] = null;
            return RedirectToAction("Login", "Home");
        }
    }
}