using System.Security.Claims;
using System.Security.Cryptography;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.DataBase.Authorization;
using TelephoneCallRecording.Services.Lockout;

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

            (string codeHash, string code) = CodeFactory.GetCodeHash();
            user.IsEmailConfirmed = false; // Заставляем пройти проверку снова, так как при каждом входе нужно подтверждать email.
            user.EmailConfirmationCodeHash = codeHash;
            await EmailService.SendConfirmationCodeAsync(user.Email, code);

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