using HuynhNgocLen.SachOnline.Models;
using HuynhNgocLen.SachOnline.Security;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Web;
using System.Text;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HuynhNgocLen.SachOnline
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.HeaderEncoding = Encoding.UTF8;
        }

        protected void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            var path = Request.Path ?? "";
            if (path.StartsWith("/Content/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Scripts/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/bundles/", StringComparison.OrdinalIgnoreCase))
                return;

            TryRestoreUserFromJwtCookie();
            EnforceCustomerJwtOrLogout();
        }

        private void TryRestoreUserFromJwtCookie()
        {
            var ctx = HttpContext.Current;
            if (ctx?.Session == null)
                return;
            if (ctx.Session["TaiKhoan"] != null)
                return;

            var cookie = ctx.Request.Cookies[JwtTokenService.CookieName];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value))
                return;

            var principal = JwtTokenService.ValidatePrincipal(cookie.Value);
            if (principal == null)
                return;

            var idStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out var maKh))
                return;

            try
            {
                using (var db = new SachOnlineEntities1())
                {
                    var kh = db.KHACHHANGs.Find(maKh);
                    if (kh != null)
                        ctx.Session["TaiKhoan"] = kh;
                }
            }
            catch
            {
                // Bỏ qua khi DB không sẵn sàng
            }
        }

        /// <summary>Hết hạn JWT (3 giờ) nhưng Session còn khách → xóa đăng nhập; với thao tác thường chuyển về trang đăng nhập.</summary>
        private void EnforceCustomerJwtOrLogout()
        {
            var ctx = HttpContext.Current;
            if (ctx?.Session == null)
                return;

            if (ctx.Session["TaiKhoan"] == null)
                return;

            var cookie = ctx.Request.Cookies[JwtTokenService.CookieName];
            var tokenOk = cookie != null && !string.IsNullOrEmpty(cookie.Value)
                && JwtTokenService.ValidatePrincipal(cookie.Value) != null;

            if (tokenOk)
                return;

            ctx.Session["TaiKhoan"] = null;
            var expired = new HttpCookie(JwtTokenService.CookieName, "")
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = ctx.Request.IsSecureConnection
            };
            ctx.Response.Cookies.Add(expired);

            var path = ctx.Request.Path ?? "";
            if (IsCustomerAuthExemptPath(path))
                return;

            var loginUrl = VirtualPathUtility.ToAbsolute("~/User/DangNhap") + "?hetHan=1";
            ctx.Response.Redirect(loginUrl, true);
        }

        private static bool IsCustomerAuthExemptPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            var p = path.ToLowerInvariant();

            if (p.Contains("/content/")
                || p.Contains("/scripts/")
                || p.Contains("/bundles/"))
                return true;

            if (p.Contains("/user/dangnhap")
                || p.Contains("/user/dangky")
                || p.Contains("/user/quenmatkhau")
                || p.Contains("/user/xacthucma")
                || p.Contains("/user/guilai")
                || p.Contains("/user/datlai"))
                return true;

            return false;
        }
    }
}
