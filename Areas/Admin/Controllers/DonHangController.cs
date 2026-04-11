using HuynhNgocLen.SachOnline.Models;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class DonHangController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        public ActionResult Index()
        {
            var listDH = db.DONDATHANGs.OrderByDescending(d => d.NgayDat).ToList();
            return View(listDH);
        }

        public ActionResult Details(int id)
        {
            var dh = db.DONDATHANGs.SingleOrDefault(d => d.MaDonHang == id);
            if (dh == null) return HttpNotFound();

            var chiTiet = db.CHITIETDATHANGs.Where(c => c.MaDonHang == id).ToList();
            ViewBag.ChiTiet = chiTiet;

            return View(dh);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var dh = db.DONDATHANGs.SingleOrDefault(d => d.MaDonHang == id);
            if (dh == null) return HttpNotFound();
            return View(dh);
        }

        [HttpPost]
        public ActionResult Edit(DONDATHANG dh)
        {
            var dhUpdate = db.DONDATHANGs.SingleOrDefault(d => d.MaDonHang == dh.MaDonHang);
            if (dhUpdate != null)
            {
                dhUpdate.TinhTrangGiaoHang = dh.TinhTrangGiaoHang;
                dhUpdate.NgayGiao = dh.NgayGiao;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(dh);
        }
    }
}