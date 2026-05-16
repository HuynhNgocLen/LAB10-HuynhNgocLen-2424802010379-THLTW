using HuynhNgocLen.SachOnline.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class DonHangController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        public ActionResult Index(string filter, System.DateTime? fromDate, System.DateTime? toDate, string pttt)
        {
            var query = db.DONDATHANGs.AsQueryable();
            var now = System.DateTime.Now;
            var today = now.Date;

            // Lọc theo mốc thời gian nhanh
            if (!string.IsNullOrEmpty(filter))
            {
                switch (filter)
                {
                    case "today":
                        query = query.Where(d => d.NgayDat >= today);
                        ViewBag.Filter = "Hôm nay";
                        break;
                    case "3days":
                        var threeDaysAgo = today.AddDays(-3);
                        query = query.Where(d => d.NgayDat >= threeDaysAgo);
                        ViewBag.Filter = "3 ngày qua";
                        break;
                    case "7days":
                        var sevenDaysAgo = today.AddDays(-7);
                        query = query.Where(d => d.NgayDat >= sevenDaysAgo);
                        ViewBag.Filter = "7 ngày qua";
                        break;
                }
            }

            // Lọc theo khoảng ngày tùy chỉnh
            if (fromDate.HasValue)
            {
                query = query.Where(d => d.NgayDat >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                var nextDay = toDate.Value.AddDays(1);
                query = query.Where(d => d.NgayDat < nextDay);
            }

            // Lọc theo phương thức thanh toán (Dùng cột thật)
            if (!string.IsNullOrEmpty(pttt))
            {
                if (pttt == "BANK") query = query.Where(d => d.PhuongThucThanhToan == "Chuyển khoản");
                else if (pttt == "MOMO") query = query.Where(d => d.PhuongThucThanhToan == "Ví MoMo");
                else if (pttt == "COD") query = query.Where(d => d.PhuongThucThanhToan == "Tiền mặt");
            }

            var listDH = query.OrderByDescending(d => d.NgayDat).ToList();
            
            ViewBag.CurrentFilter = filter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PTTT = pttt;
            ViewBag.Filter = ViewBag.Filter ?? "Kết quả lọc";

            return View(listDH);
        }

        public ActionResult ExportExcel(string filter, System.DateTime? fromDate, System.DateTime? toDate, string pttt)
        {
            var query = db.DONDATHANGs.AsQueryable();
            var today = System.DateTime.Now.Date;

            // Apply same filters as Index
            if (!string.IsNullOrEmpty(filter))
            {
                if (filter == "today") query = query.Where(d => d.NgayDat >= today);
                else if (filter == "3days") query = query.Where(d => d.NgayDat >= today.AddDays(-3));
                else if (filter == "7days") query = query.Where(d => d.NgayDat >= today.AddDays(-7));
            }
            if (fromDate.HasValue) query = query.Where(d => d.NgayDat >= fromDate.Value);
            if (toDate.HasValue) { var nextDay = toDate.Value.AddDays(1); query = query.Where(d => d.NgayDat < nextDay); }
            if (!string.IsNullOrEmpty(pttt))
            {
                if (pttt == "BANK") query = query.Where(d => d.PhuongThucThanhToan == "Chuyển khoản");
                else if (pttt == "MOMO") query = query.Where(d => d.PhuongThucThanhToan == "Ví MoMo");
                else if (pttt == "COD") query = query.Where(d => d.PhuongThucThanhToan == "Tiền mặt");
            }

            var list = query.OrderByDescending(d => d.NgayDat).ToList();

            var sb = new System.Text.StringBuilder();
            // Header
            sb.AppendLine("Ma Don Hang,Khach Hang,Ngay Dat,Phuong Thuc,Tong Tien,Trang Thai");

            foreach (var item in list)
            {
                var khachHang = item.KHACHHANG?.HoTen ?? "N/A";
                var ngayDat = item.NgayDat.HasValue ? item.NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "";
                var ptttValue = item.PhuongThucThanhToan ?? "Chưa rõ";
                var tongTien = item.CHITIETDATHANGs.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0));
                
                string trangThai = "";
                switch (item.TinhTrangGiaoHang)
                {
                    case 0: trangThai = "Moi dat"; break;
                    case 1: trangThai = "Cho xac thuc"; break;
                    case 2: trangThai = "Dang xu ly"; break;
                    case 3: trangThai = "Da giao"; break;
                    case 4: trangThai = "Da huy"; break;
                }

                sb.AppendLine($"{item.MaDonHang},{khachHang},{ngayDat},{ptttValue},{tongTien},{trangThai}");
            }

            // Return as CSV with UTF-8 BOM
            var data = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(data, "text/csv", $"DonHang_{System.DateTime.Now:yyyyMMdd_HHmm}.csv");
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
                dhUpdate.DaThanhToan = dh.DaThanhToan;
                db.SaveChanges();
                TempData["AdminMsg"] = "Cập nhật đơn hàng #" + dh.MaDonHang + " thành công.";
                return RedirectToAction("Index");
            }
            return View(dh);
        }

        [HttpPost]
        public ActionResult XacThucThanhToan(int id)
        {
            var dh = db.DONDATHANGs.Find(id);
            if (dh != null)
            {
                dh.DaThanhToan = true;
                dh.TinhTrangGiaoHang = 0; // Chuyển về "Mới đặt" (đã trả tiền)
                db.SaveChanges();
                TempData["AdminMsg"] = "Đã xác thực thanh toán cho đơn hàng #" + id;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult DuyetDonHang(int id)
        {
            var dh = db.DONDATHANGs.Find(id);
            if (dh != null)
            {
                dh.TinhTrangGiaoHang = 2; // Đã xác nhận / Đang xử lý
                db.SaveChanges();
                TempData["AdminMsg"] = "Đã xác nhận đơn hàng #" + id;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult HoanThanh(int id)
        {
            var dh = db.DONDATHANGs.Find(id);
            if (dh != null)
            {
                dh.TinhTrangGiaoHang = 3; // Đã giao hàng
                dh.NgayGiao = System.DateTime.Now;
                db.SaveChanges();
                TempData["AdminMsg"] = "Đơn hàng #" + id + " đã hoàn thành.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult HuyDon(int id)
        {
            var dh = db.DONDATHANGs.Find(id);
            if (dh != null)
            {
                dh.TinhTrangGiaoHang = 4; // Đã hủy
                db.SaveChanges();
                TempData["AdminMsg"] = "Đã hủy đơn hàng #" + id;
            }
            return RedirectToAction("Index");
        }
    }
}