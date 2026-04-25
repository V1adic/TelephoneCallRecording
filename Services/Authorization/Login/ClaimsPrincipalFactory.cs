using System.Security.Claims;
using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public class ClaimsPrincipalFactory
    {
        public static ClaimsPrincipal Get(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "cookie");
            return new ClaimsPrincipal(identity);
        }
    }
}
