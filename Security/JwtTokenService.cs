using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HuynhNgocLen.SachOnline.Security
{
    public static class JwtTokenService
    {
        public const string CookieName = "so_auth_jwt";

        public static string CreateToken(int maKh, string taiKhoan, string email)
        {
            var secret = ConfigurationManager.AppSettings["JwtSecret"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                throw new InvalidOperationException("JwtSecret trong Web.config phải có ít nhất 32 ký tự.");

            var issuer = ConfigurationManager.AppSettings["JwtIssuer"] ?? "SachOnline";
            var audience = ConfigurationManager.AppSettings["JwtAudience"] ?? "SachOnlineUsers";
            var expiryMinutes = int.TryParse(ConfigurationManager.AppSettings["JwtExpiryMinutes"], out var m) ? m : 180;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, maKh.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.UniqueName, taiKhoan ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal ValidatePrincipal(string tokenString)
        {
            if (string.IsNullOrWhiteSpace(tokenString))
                return null;

            var secret = ConfigurationManager.AppSettings["JwtSecret"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                return null;

            var issuer = ConfigurationManager.AppSettings["JwtIssuer"] ?? "SachOnline";
            var audience = ConfigurationManager.AppSettings["JwtAudience"] ?? "SachOnlineUsers";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var parms = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(tokenString, parms, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
