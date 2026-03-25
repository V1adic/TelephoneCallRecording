using Microsoft.Extensions.Options;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Lockout;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public interface IEmailConfirmationValidator
    {
        bool IsValidCode(User? user, string incomingCodeHash);
    }

    public class EmailConfirmationValidator: IEmailConfirmationValidator
    {
        private readonly IEmailLockoutService _emailLockoutService;

        public EmailConfirmationValidator(IEmailLockoutService emailLockoutService)
        {
            _emailLockoutService = emailLockoutService;
        }
        public bool IsValidCode(User? user, string incomingCodeHash)
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
                _emailLockoutService.ErrorAttempt(user);
                return false;
            }
            else if (user.EmailConfirmationCodeHash != incomingCodeHash)
            {
                _emailLockoutService.ErrorAttempt(user);
                return false;
            }
            else if (user.EmailConfirmationExpires < DateTime.UtcNow)
            {
                _emailLockoutService.ErrorAttempt(user);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}