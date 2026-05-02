using HuynhNgocLen.SachOnline.Models;
using HuynhNgocLen.SachOnline.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace HuynhNgocLen.SachOnline.Areas.Admin.Controllers
{
    public class HomeController : Controller
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        public ActionResult Dashboard(int? year, int? month)
        {
            if (Session["Admin"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var currentYear = DateTime.Now.Year;
            var availableYears = db.DONDATHANGs
                .Where(d => d.NgayDat.HasValue)
                .Select(d => d.NgayDat.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            if (!availableYears.Any())
            {
                availableYears.Add(currentYear);
            }

            var selectedYear = year.HasValue && availableYears.Contains(year.Value) ? year.Value : availableYears.First();
            var selectedMonth = (month.HasValue && month.Value >= 1 && month.Value <= 12) ? month : null;

            var filteredOrdersQuery = db.DONDATHANGs.Where(d => d.NgayDat.HasValue && d.NgayDat.Value.Year == selectedYear);
            if (selectedMonth.HasValue)
            {
                filteredOrdersQuery = filteredOrdersQuery.Where(d => d.NgayDat.Value.Month == selectedMonth.Value);
            }

            var filteredOrderIds = filteredOrdersQuery.Select(d => d.MaDonHang).ToList();

            ViewBag.TongDonHang = filteredOrderIds.Count;
            ViewBag.TongDoanhThu = db.CHITIETDATHANGs
                .Where(c => filteredOrderIds.Contains(c.MaDonHang))
                .Sum(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0;
            ViewBag.SoKhachHang = filteredOrdersQuery
                .Where(d => d.MaKH.HasValue)
                .Select(d => d.MaKH.Value)
                .Distinct()
                .Count();
            ViewBag.SoSach = db.SACHes.Count();

            var monthLabels = new List<string>();
            var orderData = new List<int>();
            var revenueData = new List<decimal>();

            for (int m = 1; m <= 12; m++)
            {
                monthLabels.Add($"T{m}");

                var monthlyOrders = db.DONDATHANGs
                    .Where(d => d.NgayDat.HasValue && d.NgayDat.Value.Year == selectedYear && d.NgayDat.Value.Month == m)
                    .Select(d => d.MaDonHang)
                    .ToList();

                orderData.Add(monthlyOrders.Count);

                var monthlyRevenue = db.CHITIETDATHANGs
                    .Where(c => monthlyOrders.Contains(c.MaDonHang))
                    .Sum(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0;
                revenueData.Add(monthlyRevenue);
            }

            var recentYears = availableYears.Take(5).OrderBy(y => y).ToList();
            var yearLabels = recentYears.Select(y => y.ToString()).ToList();
            var yearOrderData = new List<int>();
            var yearRevenueData = new List<decimal>();

            foreach (var y in recentYears)
            {
                var yearlyOrderIds = db.DONDATHANGs
                    .Where(d => d.NgayDat.HasValue && d.NgayDat.Value.Year == y)
                    .Select(d => d.MaDonHang)
                    .ToList();

                yearOrderData.Add(yearlyOrderIds.Count);
                yearRevenueData.Add(
                    db.CHITIETDATHANGs
                    .Where(c => yearlyOrderIds.Contains(c.MaDonHang))
                    .Sum(c => (decimal?)(c.SoLuong * c.DonGia)) ?? 0
                );
            }

            ViewBag.ChartYear = selectedYear;
            ViewBag.MonthLabels = monthLabels;
            ViewBag.OrderData = orderData;
            ViewBag.RevenueData = revenueData;
            ViewBag.YearLabels = yearLabels;
            ViewBag.YearOrderData = yearOrderData;
            ViewBag.YearRevenueData = yearRevenueData;
            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;

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
            var sTenDN = (f["UserName"] ?? string.Empty).Trim();
            var sMatKhau = f["Password"] ?? string.Empty;

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
                var ad = db.ADMINs.SingleOrDefault(n => n.TenDN == sTenDN);
                if (ad != null && Argon2PasswordHasher.Verify(sMatKhau, ad.MatKhau))
                {
                    if (!Argon2PasswordHasher.LooksLikeArgon2Hash(ad.MatKhau))
                    {
                        ad.MatKhau = Argon2PasswordHasher.HashPassword(sMatKhau);
                        try
                        {
                            db.SaveChanges();
                        }
                        catch (DbUpdateException ex)
                        {
                            if (!IsSqlStringTruncation(ex))
                                throw;
                            try
                            {
                                db.Entry(ad).Reload();
                            }
                            catch
                            {
                                var id = ad.MaAd;
                                db.Entry(ad).State = EntityState.Detached;
                                ad = db.ADMINs.Single(a => a.MaAd == id);
                            }
                            TempData["ThongBaoAdmin"] =
                                "Cột ADMIN.MatKhau trong SQL Server vẫn quá ngắn. Chạy ALTER trong App_Data/AuthSchemaUpdate.sql hoặc AdminMatKhauArgon2.sql, rồi đăng nhập lại.";
                        }
                    }
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

        /// <summary>
        /// SQL Server classic dùng 8152; từ 2019 thường dùng 2628 cho lỗi cắt chuỗi.
        /// </summary>
        private static bool IsSqlStringTruncation(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is SqlException sql)
                {
                    if (sql.Number == 8152 || sql.Number == 2628)
                        return true;
                    if (!string.IsNullOrEmpty(sql.Message)
                        && sql.Message.IndexOf("truncated", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }
    }
}