using HuynhNgocLen.SachOnline.Models;
using PagedList;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class SachController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        // DANH SÁCH SÁCH
        public ActionResult Index(int? page, string tuKhoa)
        {
            int pageSize = 20;
            int pageNumber = (page ?? 1);

            ViewBag.TuKhoa = tuKhoa;

            var listSach = db.SACHes.OrderByDescending(s => s.NgayCapNhat).AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                listSach = listSach.Where(s => s.TenSach.Contains(tuKhoa));
            }

            return View(listSach.ToPagedList(pageNumber, pageSize));
        }

        // THÊM SÁCH
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.MaCD = new SelectList(db.CHUDEs.ToList(), "MaCD", "TenChuDe");
            ViewBag.MaNXB = new SelectList(db.NHAXUATBANs.ToList(), "MaNXB", "TenNXB");
            return View();
        }

        // THÊM SÁCH
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(SACH sach, HttpPostedFileBase fFileUpload)
        {
            ViewBag.MaCD = new SelectList(db.CHUDEs.ToList(), "MaCD", "TenChuDe");
            ViewBag.MaNXB = new SelectList(db.NHAXUATBANs.ToList(), "MaNXB", "TenNXB");

            if (ModelState.IsValid)
            {
                if (fFileUpload == null || fFileUpload.ContentLength == 0)
                {
                    ViewBag.ThongBao = "Vui lòng chọn ảnh bìa cho sách!";
                    return View(sach);
                }

                var fileName = Path.GetFileName(fFileUpload.FileName);

                if (fileName.Length > 50)
                {
                    ViewBag.ThongBao = $"Tên file ảnh quá dài ({fileName.Length} ký tự). Vui lòng đổi tên file ảnh dưới 50 ký tự!";
                    return View(sach);
                }

                var path = Path.Combine(Server.MapPath("~/Images/"), fileName);

                if (System.IO.File.Exists(path))
                {
                    ViewBag.ThongBao = "Hình ảnh này đã tồn tại trên Server. Vui lòng đổi tên file khác!";
                    return View(sach);
                }
                else
                {
                    fFileUpload.SaveAs(path);
                    sach.AnhBia = fileName;
                }

                sach.NgayCapNhat = DateTime.Now;
                db.SACHes.Add(sach);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(sach);
        }

        // SỬA SÁCH
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var sach = db.SACHes.SingleOrDefault(n => n.MaSach == id);
            if (sach == null) return HttpNotFound();

            ViewBag.MaCD = new SelectList(db.CHUDEs.ToList(), "MaCD", "TenChuDe", sach.MaCD);
            ViewBag.MaNXB = new SelectList(db.NHAXUATBANs.ToList(), "MaNXB", "TenNXB", sach.MaNXB);
            return View(sach);
        }

        // SỬA SÁCH
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(SACH sach, HttpPostedFileBase fFileUpload)
        {
            if (ModelState.IsValid)
            {
                var sachUpdate = db.SACHes.SingleOrDefault(n => n.MaSach == sach.MaSach);
                if (sachUpdate != null)
                {
                    if (fFileUpload != null && fFileUpload.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(fFileUpload.FileName);
                        var path = Path.Combine(Server.MapPath("~/Images/"), fileName);
                        if (!System.IO.File.Exists(path))
                        {
                            fFileUpload.SaveAs(path);
                        }
                        sachUpdate.AnhBia = fileName;
                    }

                    sachUpdate.TenSach = sach.TenSach;
                    sachUpdate.MoTa = sach.MoTa;
                    sachUpdate.SoLuongBan = sach.SoLuongBan;
                    sachUpdate.GiaBan = sach.GiaBan;
                    sachUpdate.MaCD = sach.MaCD;
                    sachUpdate.MaNXB = sach.MaNXB;
                    sachUpdate.NgayCapNhat = DateTime.Now;

                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            ViewBag.MaCD = new SelectList(db.CHUDEs.ToList(), "MaCD", "TenChuDe", sach.MaCD);
            ViewBag.MaNXB = new SelectList(db.NHAXUATBANs.ToList(), "MaNXB", "TenNXB", sach.MaNXB);
            return View(sach);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var sach = db.SACHes.SingleOrDefault(n => n.MaSach == id);
            if (sach == null)
            {
                return HttpNotFound();
            }
            return View(sach);
        }

        // XÓA SÁCH
        [HttpPost, ActionName("Delete")]
        public ActionResult ConfirmDelete(int id)
        {
            var sach = db.SACHes.SingleOrDefault(n => n.MaSach == id);
            if (sach == null)
            {
                return HttpNotFound();
            }

            db.SACHes.Remove(sach);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
