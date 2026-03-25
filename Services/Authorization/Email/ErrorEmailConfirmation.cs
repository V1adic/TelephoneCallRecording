using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Lockout;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public class ErrorEmailConfirmation
    {
        public static bool IsValidCode(User? user, string incomingCodeHash)
        {
            if (user == null)
            {
                return false;
            }
            else if (EmailLockoutService.AttemptLockout(user))
            {
                return false;
            }
            else if (user.IsEmailConfirmed)
            {
                EmailLockoutService.ErrorAttempt(user);
                return false;
            }
            else if (user.EmailConfirmationCodeHash != incomingCodeHash)
            {
                EmailLockoutService.ErrorAttempt(user);
                return false;
            }
            else if (user.EmailConfirmationExpires < DateTime.UtcNow)
            {
                EmailLockoutService.ErrorAttempt(user);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
