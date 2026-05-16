using HuynhNgocLen.SachOnline.Models;
using PagedList;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class NhaXuatBanController : Controller
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

        // DANH SÁCH NXB (có tìm kiếm + phân trang, mỗi trang 15)
        public ActionResult Index(int? page, string tuKhoa)
        {
            int pageSize = 15;
            int pageNumber = (page ?? 1);

            ViewBag.TuKhoa = tuKhoa;

            var listNXB = db.NHAXUATBANs.AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                listNXB = listNXB.Where(n => n.TenNXB.Contains(tuKhoa));
            }

            return View(listNXB.OrderBy(n => n.MaNXB).ToPagedList(pageNumber, pageSize));
        }

        // THÊM NXB
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // THÊM NXB
        [HttpPost]
        public ActionResult Create(NHAXUATBAN nxb)
        {
            if (ModelState.IsValid)
            {
                db.NHAXUATBANs.Add(nxb);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nxb);
        }

        // SỬA NXB
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var nxb = db.NHAXUATBANs.SingleOrDefault(n => n.MaNXB == id);
            if (nxb == null) return HttpNotFound();
            return View(nxb);
        }

        // SỬA NXB
        [HttpPost]
        public ActionResult Edit(NHAXUATBAN nxb)
        {
            if (ModelState.IsValid)
            {
                var nxbUpdate = db.NHAXUATBANs.SingleOrDefault(n => n.MaNXB == nxb.MaNXB);
                if (nxbUpdate != null)
                {
                    nxbUpdate.TenNXB = nxb.TenNXB;
                    nxbUpdate.DiaChi = nxb.DiaChi;
                    nxbUpdate.DienThoai = nxb.DienThoai;
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            return View(nxb);
        }

        // XÓA NXB
        [HttpGet]
        public ActionResult Delete(int id)
        {
            return RedirectToAction("Index");
        }

        // XÓA NXB
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDelete(int id)
        {
            var nxb = db.NHAXUATBANs.SingleOrDefault(n => n.MaNXB == id);
            if (nxb == null) return RedirectToAction("Index");

            try
            {
                db.NHAXUATBANs.Remove(nxb);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ThongBaoAdmin"] = "Không thể xóa nhà xuất bản vì đang có dữ liệu liên quan.";
                return RedirectToAction("Index");
            }
        }
    }
}
