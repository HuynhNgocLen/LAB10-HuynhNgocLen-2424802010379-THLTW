using HuynhNgocLen.SachOnline.Models;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class NhaXuatBanController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        // DANH SÁCH NXB
        public ActionResult Index()
        {
            return View(db.NHAXUATBANs.ToList());
        }

        // THÊM NXB
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        //THÊM NXB
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
            var nxb = db.NHAXUATBANs.SingleOrDefault(n => n.MaNXB == id);
            if (nxb == null) return HttpNotFound();
            return View(nxb);
        }

        //  XÓA NXB
        [HttpPost, ActionName("Delete")]
        public ActionResult ConfirmDelete(int id)
        {
            var nxb = db.NHAXUATBANs.SingleOrDefault(n => n.MaNXB == id);
            if (nxb != null)
            {
                db.NHAXUATBANs.Remove(nxb);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}