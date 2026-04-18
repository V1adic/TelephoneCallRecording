using System.Security.Cryptography;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Lockout;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public interface IEmailConfirmationValidator
    {
        bool IsValidCode(User? user, string incomingCodeHash);
    }

    public class EmailConfirmationValidator : IEmailConfirmationValidator
    {
        private readonly IEmailLockoutService _emailLockoutService;

        public EmailConfirmationValidator(IEmailLockoutService emailLockoutService)
        {
            _emailLockoutService = emailLockoutService;
        }

        public bool IsValidCode(User? user, string incomingCodeHash)
        {
            if (user == null || user.IsEmailConfirmed)
            {
                return false;
            }

            if (EmailLockoutService.AttemptLockout(user))
            {
                return false;
            }

            if (user.EmailConfirmationCodeHash is null || user.EmailConfirmationExpires is null)
            {
                _emailLockoutService.ErrorAttempt(user);
                return false;
            }

            if (user.EmailConfirmationExpires < DateTime.UtcNow)
            {
                _emailLockoutService.ErrorAttempt(user);
                return false;
            }

            var storedHashBytes = Convert.FromBase64String(user.EmailConfirmationCodeHash);
            var incomingHashBytes = Convert.FromBase64String(incomingCodeHash);
            if (!CryptographicOperations.FixedTimeEquals(storedHashBytes, incomingHashBytes))
            {
                _emailLockoutService.ErrorAttempt(user);
                return false;
            }

            return true;
        }
    }
}
