using Microsoft.Extensions.Options;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;

namespace TelephoneCallRecording.Services.Authorization.Lockout
{
    public interface IEmailLockoutService
    {
        void ErrorAttempt(User user);
    }

    public class EmailLockoutService : IEmailLockoutService
    {
        private readonly IOptions<LockoutOptions> _options;

        public EmailLockoutService(IOptions<LockoutOptions> options)
        {
            _options = options;
        }

        public static bool AttemptLockout(User user)
        {
            var accountLocked = false;
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                accountLocked = true;
            }
            else if (user.LockoutEnd.HasValue)
            {
                ErrorReset(user);
            }

            return accountLocked;
        }

        public void ErrorAttempt(User user)
        {
            user.FailedEmailConfirmAttempts++;
            if (user.FailedEmailConfirmAttempts >= _options.Value.MaxFailedConfirmAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(_options.Value.LockoutDurationMinutes);
            }
        }

        private static void ErrorReset(User user)
        {
            user.FailedEmailConfirmAttempts = 0;
            user.LockoutEnd = null;
        }
    }
}
