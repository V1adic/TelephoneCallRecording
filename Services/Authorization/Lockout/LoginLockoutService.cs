using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Lockout
{
    public class LoginLockoutService
    {
        const int MaxAttempts = 5;
        const int LockoutDurationMinutes = 15;
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

        // Метод для сброса счетчика неудачных попыток и блокировки
        static public void ErrorReset(User user)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        // Метод для обработки неправильного пароля
        static public void PasswordIncorrect(User user)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
            }
        }
    }
}
