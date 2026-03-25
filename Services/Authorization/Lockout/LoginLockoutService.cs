using Microsoft.Extensions.Options;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;

namespace TelephoneCallRecording.Services.Authorization.Lockout
{
    public interface ILoginLockoutService
    {
        void PasswordIncorrect(User user);
    }
    public class LoginLockoutService: ILoginLockoutService
    {
        private readonly IOptions<LockoutOptions> _options;
        public LoginLockoutService(IOptions<LockoutOptions> options)
        {
            _options = options;
        }
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
        public void PasswordIncorrect(User user)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _options.Value.MaxFailedLoginAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(_options.Value.LockoutDurationMinutes);
            }
        }
    }
}
