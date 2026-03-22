using System.Security.Cryptography;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Cryptography.Authorization;
using TelephoneCallRecording.Services.Lockout;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public class PasswordValidator
    {

        private static readonly string DummySalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        private static readonly string DummyHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Метод для проверки пароля
        static public bool AttemptPassword(User user, string password, bool accountLocked)
        {
            string hashToUse = (user == null || accountLocked) ? DummyHash : user.PasswordHash;
            string saltToUse = (user == null || accountLocked) ? DummySalt : user.PasswordSalt;

            // Проверяем пароль, используя реальные или фиктивные данные
            bool passwordCorrect = PasswordHasher.Verify(password, hashToUse, saltToUse);

            if (accountLocked || user == null)
                return false;

            if (!passwordCorrect)
            {
                LoginLockoutService.PasswordIncorrect(user);
                return false;
            }

            LoginLockoutService.ErrorReset(user);

            return true;
        }
    }
}
