using System.Security.Claims;
using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public class ClaimsPrincipalFactory
    {
        static public ClaimsPrincipal Get(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "cookie");
            return new ClaimsPrincipal(identity);
        }
    }
}