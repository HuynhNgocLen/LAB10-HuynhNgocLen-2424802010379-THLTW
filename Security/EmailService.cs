using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

namespace HuynhNgocLen.SachOnline.Security
{
    public static class EmailService
    {
        public static bool IsConfigured()
        {
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var from = ConfigurationManager.AppSettings["MailFrom"];
            return !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(from);
        }

        public static bool TrySend(string to, string subject, string body, out string error)
        {
            return TrySend(to, subject, body, false, out error);
        }

        public static bool TrySend(string to, string subject, string body, bool isHtml, out string error)
        {
            error = null;
            if (!IsConfigured())
            {
                error = "Chưa cấu hình SMTP (SmtpHost, MailFrom) trong Web.config.";
                return false;
            }

            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var port = int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out var p) ? p : 587;
            var ssl = !bool.TryParse(ConfigurationManager.AppSettings["SmtpSsl"], out var s) || s;
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPassword"];
            var from = ConfigurationManager.AppSettings["MailFrom"];

            try
            {
                using (var msg = new MailMessage(from, to, subject, body))
                {
                    msg.IsBodyHtml = isHtml;
                    using (var client = new SmtpClient(host, port))
                    {
                        client.EnableSsl = ssl;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        if (!string.IsNullOrWhiteSpace(user))
                            client.Credentials = new NetworkCredential(user, pass);

                        client.Send(msg);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Trace.TraceWarning("SMTP gửi thất bại: {0}", ex);
                return false;
            }
        }
    }
}

