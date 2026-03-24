using System.Security.Claims;
using System.Security.Cryptography;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Lockout;
using TelephoneCallRecording.Services.DataBase.Authorization;


namespace TelephoneCallRecording.Services.Authorization.Login
{
    public class LoginService
    {
        static public async Task<ClaimsPrincipal?> AttemptLogin(AppDbContext _db, string username, string password)
        {
            var user = await FindUserService.SearchByUsername(_db, username);

            if (user == null)
                return null;

            bool accountLocked = LoginLockoutService.AttemptLockout(user);
            bool passwordValid = PasswordValidator.AttemptPassword(user, password, accountLocked);

            if(!UserVerification.Check(user))
            {
                await UserVerification.Verification(user);
            }

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