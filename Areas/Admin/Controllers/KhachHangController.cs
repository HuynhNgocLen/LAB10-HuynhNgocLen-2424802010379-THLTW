using HuynhNgocLen.SachOnline.Models;
using PagedList;
using System.Drawing.Printing;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class KhachHangController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ad = Session["Admin"] as ADMIN;
            if (ad == null || ad.Quyen != 1)
            {
                TempData["ThongBaoAdmin"] = "Bạn không có quyền truy cập chức năng này!";
                filterContext.Result = RedirectToAction("Dashboard", "Home");
            }
            base.OnActionExecuting(filterContext);
        }

        // DANH SÁCH KHÁCH HÀNG
        public ActionResult Index(int? page, string tuKhoa)
        {
            int pageSize = 20;
            int pageNumber = (page ?? 1);

            ViewBag.TuKhoa = tuKhoa;

            var listKH = db.KHACHHANGs.AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                listKH = listKH.Where(c => c.HoTen.Contains(tuKhoa));
            }

            return View(listKH.OrderBy(c => c.MaKH).ToPagedList(pageNumber, pageSize));
        }

        // THÊM KHÁCH HÀNG
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        //THÊM KHÁCH HÀNG
        [HttpPost]
        public ActionResult Create(KHACHHANG kh)
        {
            if (ModelState.IsValid)
            {
                var checkTK = db.KHACHHANGs.FirstOrDefault(k => k.TaiKhoan == kh.TaiKhoan);
                if (checkTK != null)
                {
                    ViewBag.ThongBao = "Tài khoản này đã tồn tại. Vui lòng chọn tên đăng nhập khác!";
                    return View(kh);
                }

                db.KHACHHANGs.Add(kh);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(kh);
        }

        // SỬA KHÁCH HÀNG
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var kh = db.KHACHHANGs.SingleOrDefault(n => n.MaKH == id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }

        // SỬA KHÁCH HÀNG
        [HttpPost]
        public ActionResult Edit(KHACHHANG kh)
        {
            if (ModelState.IsValid)
            {
                var khUpdate = db.KHACHHANGs.SingleOrDefault(n => n.MaKH == kh.MaKH);
                if (khUpdate != null)
                {
                    khUpdate.HoTen = kh.HoTen;
                    khUpdate.Email = kh.Email;
                    khUpdate.DiaChi = kh.DiaChi;
                    khUpdate.DienThoai = kh.DienThoai;
                    khUpdate.NgaySinh = kh.NgaySinh;

                    if (!string.IsNullOrEmpty(kh.MatKhau))
                    {
                        khUpdate.MatKhau = kh.MatKhau;
                    }

                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            return View(kh);
        }

        // XÓA KHÁCH HÀNG
        [HttpGet]
        public ActionResult Delete(int id)
        {
            return RedirectToAction("Index");
        }

        // XÓA KHÁCH HÀNG
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDelete(int id)
        {
            var kh = db.KHACHHANGs.SingleOrDefault(n => n.MaKH == id);
            if (kh != null)
            {
                try
                {
                    db.KHACHHANGs.Remove(kh);
                    db.SaveChanges();
                }
                catch
                {
                    TempData["ThongBaoAdmin"] = "Không thể xóa khách hàng này vì họ đã có Đơn đặt hàng trong hệ thống!";
                }
            }
            return RedirectToAction("Index");
        }
    }
}