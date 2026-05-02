using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HuynhNgocLen.SachOnline.Models;
using PagedList;
using PagedList.Mvc;

namespace SachOnline.Controllers
{
    public class SachOnlineController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();
        private List<SACH> LaySachMoi(int soluong)
        {
            return db.SACHes.OrderByDescending(n => n.NgayCapNhat).Take(soluong).ToList();
        }

        // Trang chủ
        public ActionResult Index(int page = 1)
        {
            int size = 6;
            var listSachMoi = db.SACHes.OrderByDescending(n => n.NgayCapNhat).Take(20).ToList();
            return View(listSachMoi.ToPagedList(page, size));
        }

        // 1. Menu trên
        [ChildActionOnly]
        public ActionResult NavPartial()
        {
            return PartialView();
        }

        // 2. Banner quảng cáo
        public ActionResult SliderPartial()
        {
            return PartialView();
        }

        // 3. Menu Chủ đề
        public ActionResult ChuDePartial()
        {
            var listChuDe = db.CHUDEs.ToList();
            return PartialView(listChuDe);
        }

        // 4. Menu Nhà xuất bản
        public ActionResult NhaXuatBanPartial()
        {
            var listNXB = db.NHAXUATBANs.ToList();
            return PartialView(listNXB);
        }

        // 5. Chân trang
        public ActionResult FooterPartial()
        {
            return PartialView();
        }

        // 6. Sách bán nhiều
        public ActionResult SachBanNhieuPartial()
        {
            var listSachBanNhieu = db.SACHes.OrderByDescending(s => s.SoLuongBan).Take(6).ToList();
            return PartialView(listSachBanNhieu);
        }

        // 7. View Sách theo chủ đề
        public ActionResult SachTheoChuDe(int id, int page = 1)
        {
            int size = 3;
            var CD = db.CHUDEs.SingleOrDefault(x => x.MaCD == id);
            if (CD != null)
            {
                ViewBag.CD = CD.TenChuDe;
            }
            else
            {
                ViewBag.CD = "Không tìm thấy chủ đề";
            }

            ViewBag.MaCD = id;

            var kq = (from s in db.SACHes where s.MaCD == id select s).ToList();
            return View(kq.ToPagedList(page, size));
        }

        //8. View sach theo nhà xuất bản
        public ActionResult SachTheoNXB(int id, int page = 1)
        {
            int size = 3;
            var NXB = db.NHAXUATBANs.SingleOrDefault(x => x.MaNXB == id);
            if(NXB != null)
            {
                ViewBag.NXB = NXB.TenNXB;
            }
            else
            {
                ViewBag.NXB = "Không tìm thấy nhà xuất bản";
            }
            ViewBag.MaNXB = id;

            var kq = (from s in db.SACHes where s.MaNXB == id select s).ToList();
            return View(kq.ToPagedList(page,size));
        }

        //9. Chi tiết sách
        public ActionResult ChiTietSach(int id)
        {
            var sach = db.SACHes.SingleOrDefault(s => s.MaSach == id);

            if (sach == null)
            {
                return HttpNotFound();
            }

            if (sach.MaCD.HasValue)
            {
                ViewBag.SachLienQuan = db.SACHes
                    .Where(s => s.MaCD == sach.MaCD && s.MaSach != id)
                    .OrderByDescending(s => s.NgayCapNhat)
                    .Take(4)
                    .ToList();
            }
            else
            {
                ViewBag.SachLienQuan = new List<SACH>();
            }

            return View(sach);
        }

        public ActionResult GioiThieu()
        {
            return View();
        }

        public ActionResult LienHe()
        {
            return View();
        }

        //10. LoginLogout Partial View
        [ChildActionOnly]
        public ActionResult LoginLogout()
        {
            return PartialView("LoginLogoutPartial");
        }

        //11. Tìm kiếm sách
        public ActionResult Search(string data, int page = 1)
        {
            int size = 6;

            if (string.IsNullOrWhiteSpace(data))
                return RedirectToAction("Index");

            data = data.Trim();
            var kq = db.SACHes
                       .Where(s => s.TenSach != null && s.TenSach.Contains(data))
                       .OrderByDescending(s => s.NgayCapNhat)
                       .ToList();

            ViewBag.TuKhoa = data;
            ViewBag.SoKetQua = kq.Count;
            return View(kq.ToPagedList(page, size));
        }
    }
}