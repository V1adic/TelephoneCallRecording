using System.Security.Claims;
using System.Security.Cryptography;
using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public class LoginService
    {
        static public async Task<ClaimsPrincipal?> AttemptLogin(AppDbContext _db, string username, string password)
        {
            var user = await FindUserService.SearchByUsername(_db, username);

            if (user == null)
                return null;

            bool accountLocked = AccountLockoutService.AttemptLockout(user);
            bool passwordValid = PasswordValidator.AttemptPassword(user, password, accountLocked);
            await _db.SaveChangesAsync();

            if (passwordValid)
            {
                return ClaimsPrincipalFactory.Get(user);
            }
            else
            {
                return null;
            }
        }
    }
}