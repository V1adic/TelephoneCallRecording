using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public class AccountLockoutService
    {
        static public bool AttemptLockout(User user)
        {
            bool accountLocked = false;

            // Проверяем не заблокирован ли пользователь и разблокируем его, если срок блокировки истек
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
        static public void ErrorReset(User user)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }
    }
}
