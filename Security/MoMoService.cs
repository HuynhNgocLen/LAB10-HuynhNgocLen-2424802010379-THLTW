using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HuynhNgocLen.SachOnline.Services
{
    /// <summary>
    /// Service tích hợp MoMo Payment Gateway (API v2 - captureWallet).
    /// Dùng môi trường Test mặc định (test-payment.momo.vn).
    /// </summary>
    public static class MoMoService
    {
        // ── Cấu hình đọc từ Web.config ───────────────────────────────
        private static string Endpoint =>
            ConfigurationManager.AppSettings["MoMoEndpoint"]
            ?? "https://test-payment.momo.vn/v2/gateway/api/create";

        private static string PartnerCode =>
            ConfigurationManager.AppSettings["MoMoPartnerCode"] ?? "";

        private static string AccessKey =>
            ConfigurationManager.AppSettings["MoMoAccessKey"] ?? "";

        private static string SecretKey =>
            ConfigurationManager.AppSettings["MoMoSecretKey"] ?? "";

        // ── Tạo yêu cầu thanh toán ──────────────────────────────────
        /// <summary>
        /// Gửi yêu cầu tạo giao dịch đến MoMo.
        /// Trả về payUrl để redirect user, hoặc null nếu lỗi.
        /// </summary>
        public static MoMoCreateResponse CreatePayment(
            string orderId,
            long amount,
            string orderInfo,
            string redirectUrl,
            string ipnUrl,
            string extraData = "")
        {
            var requestId = orderId; // dùng cùng orderId cho đơn giản

            // ── Tạo rawSignature ──
            var rawSignature =
                $"accessKey={AccessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={ipnUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={PartnerCode}" +
                $"&redirectUrl={redirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType=captureWallet";

            var signature = HmacSha256(rawSignature, SecretKey);

            // ── Body request ──
            var body = new
            {
                partnerCode = PartnerCode,
                partnerName = "SachOnline",
                storeId = PartnerCode,
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = "captureWallet",
                signature = signature
            };

            var jsonBody = JsonConvert.SerializeObject(body);

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(Endpoint);
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Timeout = 30000;

                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(jsonBody);
                }

                string responseStr;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    responseStr = reader.ReadToEnd();
                }

                var json = JObject.Parse(responseStr);
                return new MoMoCreateResponse
                {
                    ResultCode = json["resultCode"]?.Value<int>() ?? -1,
                    Message = json["message"]?.Value<string>(),
                    PayUrl = json["payUrl"]?.Value<string>(),
                    Deeplink = json["deeplink"]?.Value<string>(),
                    QrCodeUrl = json["qrCodeUrl"]?.Value<string>()
                };
            }
            catch (Exception ex)
            {
                return new MoMoCreateResponse
                {
                    ResultCode = -1,
                    Message = "Lỗi kết nối MoMo: " + ex.Message
                };
            }
        }

        // ── Xác thực chữ ký IPN / Redirect ──────────────────────────
        /// <summary>
        /// Verify signature của MoMo trả về (IPN hoặc redirect).
        /// </summary>
        public static bool VerifySignature(
            string partnerCode, string orderId, string requestId,
            long amount, string orderInfo, string orderType,
            long transId, int resultCode, string message,
            string payType, long responseTime, string extraData,
            string receivedSignature)
        {
            var rawSignature =
                $"accessKey={AccessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&message={message}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&orderType={orderType}" +
                $"&partnerCode={partnerCode}" +
                $"&payType={payType}" +
                $"&requestId={requestId}" +
                $"&responseTime={responseTime}" +
                $"&resultCode={resultCode}" +
                $"&transId={transId}";

            var computed = HmacSha256(rawSignature, SecretKey);
            return string.Equals(computed, receivedSignature, StringComparison.OrdinalIgnoreCase);
        }

        // ── HMAC SHA256 ──────────────────────────────────────────────
        private static string HmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hash = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }

    public class MoMoCreateResponse
    {
        public int ResultCode { get; set; }
        public string Message { get; set; }
        public string PayUrl { get; set; }
        public string Deeplink { get; set; }
        public string QrCodeUrl { get; set; }
        public bool Success => ResultCode == 0 && !string.IsNullOrEmpty(PayUrl);
    }
}
