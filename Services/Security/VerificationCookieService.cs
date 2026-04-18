using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;

namespace TelephoneCallRecording.Services.Security
{
    public interface IVerificationCookieService
    {
        void Issue(HttpContext context, string username);
        bool TryGetUsername(HttpContext context, out string username);
        void Clear(HttpContext context);
    }

    public class VerificationCookieService : IVerificationCookieService
    {
        private const string CookieName = "email_verify";
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _environment;
        private readonly LockoutOptions _options;

        public VerificationCookieService(
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment environment,
            IOptions<LockoutOptions> options)
        {
            _protector = dataProtectionProvider.CreateProtector("email-verification-cookie");
            _environment = environment;
            _options = options.Value;
        }

        public void Issue(HttpContext context, string username)
        {
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.CodeExpirationMinutes);
            var payload = $"{username}|{expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}";
            var protectedPayload = _protector.Protect(payload);

            context.Response.Cookies.Append(
                CookieName,
                protectedPayload,
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = !_environment.IsDevelopment(),
                    Expires = expiresAt,
                    Path = "/auth"
                });
        }

        public bool TryGetUsername(HttpContext context, out string username)
        {
            username = string.Empty;
            var cookie = context.Request.Cookies[CookieName];
            if (string.IsNullOrWhiteSpace(cookie))
            {
                return false;
            }

            try
            {
                var payload = _protector.Unprotect(cookie);
                var parts = payload.Split('|', 2);
                if (parts.Length != 2)
                {
                    return false;
                }

                if (!long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresUnix))
                {
                    return false;
                }

                if (DateTimeOffset.UtcNow > DateTimeOffset.FromUnixTimeSeconds(expiresUnix))
                {
                    return false;
                }

                username = parts[0];
                return !string.IsNullOrWhiteSpace(username);
            }
            catch
            {
                return false;
            }
        }

        public void Clear(HttpContext context)
        {
            context.Response.Cookies.Delete(CookieName, new CookieOptions
            {
                Path = "/auth",
                SameSite = SameSiteMode.Strict,
                Secure = !_environment.IsDevelopment()
            });
        }
    }
}
