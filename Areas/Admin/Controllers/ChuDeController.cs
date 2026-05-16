using HuynhNgocLen.SachOnline.Models;
using PagedList;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class ChuDeController : Controller
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

        public ActionResult Index(int? page, string tuKhoa)
        {
            int pageSize = 15;
            int pageNumber = (page ?? 1);

            ViewBag.TuKhoa = tuKhoa;

            var listChuDe = db.CHUDEs.AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                listChuDe = listChuDe.Where(c => c.TenChuDe.Contains(tuKhoa));
            }

            return View(listChuDe.OrderBy(c => c.MaCD).ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(CHUDE chude)
        {
            if (ModelState.IsValid)
            {
                db.CHUDEs.Add(chude);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(chude);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var cd = db.CHUDEs.SingleOrDefault(c => c.MaCD == id);
            if (cd == null) return HttpNotFound();
            return View(cd);
        }

        [HttpPost]
        public ActionResult Edit(CHUDE chude)
        {
            if (ModelState.IsValid)
            {
                var cdUpdate = db.CHUDEs.SingleOrDefault(c => c.MaCD == chude.MaCD);
                if (cdUpdate != null)
                {
                    cdUpdate.TenChuDe = chude.TenChuDe;
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            return View(chude);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDelete(int id)
        {
            var cd = db.CHUDEs.SingleOrDefault(c => c.MaCD == id);
            if (cd == null) return RedirectToAction("Index");

            try
            {
                db.CHUDEs.Remove(cd);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ThongBaoAdmin"] = "Không thể xóa chủ đề vì đang có dữ liệu liên quan.";
                return RedirectToAction("Index");
            }
        }
    }
}
