using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Lockout
{
    public class EmailLockoutService
    {
        static public bool AttemptLockout(User user)
        {
            bool accountLocked = false;
            // Проверяем не заблокирован ли пользователь и разблокируем его, если срок блокировки истек
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                accountLocked = true;
            }
            else if (user.EmailConfirmationCodeHash != null)
            {
                ErrorReset(user);
            }
            return accountLocked;
        }

        static public void ErrorAttempt(User user)
        {
            user.FailedEmailConfirmAttempts++;
            if (user.FailedEmailConfirmAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
            }
        }

        static private void ErrorReset(User user)
        {
            user.FailedEmailConfirmAttempts = 0;
            user.LockoutEnd = null;
        }
    }
}
