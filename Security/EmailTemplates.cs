using System.Web;

namespace HuynhNgocLen.SachOnline.Security
{
    /// <summary>HTML gửi email OTP — inline CSS tương thích client mail.</summary>
    public static class EmailTemplates
    {
        private const string Brand = "SachOnline";
        private const string Accent = "#c45c26";
        private const string Bg = "#f4f2ef";
        private const string Text = "#2d3748";

        public static string RegistrationOtpHtml(string code, int digitCount, int validMinutes)
        {
            return Wrap(
                "Xác thực đăng ký",
                "Bạn đang hoàn tất đăng ký tài khoản. Dùng mã bên dưới trên trang xác thực của website.",
                code,
                digitCount,
                validMinutes,
                "Nếu bạn không đăng ký tài khoản, hãy bỏ qua email này.");
        }

        public static string ResetPasswordOtpHtml(string code, int digitCount, int validMinutes)
        {
            return Wrap(
                "Đặt lại mật khẩu",
                "Có yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nhập mã sau để tiếp tục.",
                code,
                digitCount,
                validMinutes,
                "Nếu không phải bạn, vui lòng bỏ qua và mật khẩu hiện tại vẫn an toàn.");
        }

        private static string Wrap(string title, string intro, string code, int digitCount, int validMinutes, string footer)
        {
            var safeCode = HttpUtility.HtmlEncode(code ?? string.Empty);
            var safeTitle = HttpUtility.HtmlEncode(title);
            var safeIntro = HttpUtility.HtmlEncode(intro);
            var safeFooter = HttpUtility.HtmlEncode(footer);

            return @"<!DOCTYPE html>
<html lang=""vi"">
<head><meta charset=""utf-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/></head>
<body style=""margin:0;padding:0;background:" + Bg + @";font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:" + Text + @";"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:" + Bg + @";padding:28px 16px;"">
<tr><td align=""center"">
<table role=""presentation"" width=""100%"" style=""max-width:520px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(45,55,72,.08);"">
<tr><td style=""padding:28px 32px 8px;text-align:center;border-bottom:3px solid " + Accent + @";"">
<div style=""font-size:22px;font-weight:700;letter-spacing:.02em;color:" + Accent + @";"">📚 " + HttpUtility.HtmlEncode(Brand) + @"</div>
</td></tr>
<tr><td style=""padding:28px 32px 8px;"">
<h1 style=""margin:0 0 12px;font-size:20px;font-weight:600;color:" + Text + @";"">" + safeTitle + @"</h1>
<p style=""margin:0 0 24px;font-size:15px;line-height:1.55;color:#4a5568;"">" + safeIntro + @"</p>
<div style=""text-align:center;padding:20px 16px;background:linear-gradient(180deg,#faf8f5 0%,#fff 100%);border-radius:12px;border:1px solid #e8e4df;"">
<p style=""margin:0 0 8px;font-size:13px;text-transform:uppercase;letter-spacing:.12em;color:#718096;"">Mã xác thực (" + digitCount + @" số)</p>
<div style=""font-size:32px;font-weight:700;letter-spacing:" + (digitCount <= 4 ? "8px" : "4px") + @";color:" + Accent + @";font-variant-numeric:tabular-nums;"">" + safeCode + @"</div>
<p style=""margin:16px 0 0;font-size:13px;color:#718096;"">Hiệu lực <strong>" + validMinutes + @" phút</strong></p>
</div>
</td></tr>
<tr><td style=""padding:0 32px 28px;"">
<p style=""margin:0;font-size:13px;line-height:1.5;color:#a0aec0;"">" + safeFooter + @"</p>
</td></tr>
<tr><td style=""padding:16px 32px;background:#fafaf9;font-size:12px;color:#a0aec0;text-align:center;line-height:1.45;"">
" + HttpUtility.HtmlEncode(Brand) + @" · Email tự động, không trả lời trực tiếp.
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }
    }
}
