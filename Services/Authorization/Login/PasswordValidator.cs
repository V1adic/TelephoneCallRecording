using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Lockout;
using TelephoneCallRecording.Services.Cryptography.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public interface IPasswordValidator
    {
        bool AttemptPassword(User user, string password, bool accountLocked);
    }

    public class PasswordValidator : IPasswordValidator
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILoginLockoutService _loginLockoutService;

        private static readonly string DummySalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        private static readonly string DummyHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        public PasswordValidator(IPasswordHasher passwordHasher, ILoginLockoutService loginLockoutService)
        {
            _passwordHasher = passwordHasher;
            _loginLockoutService = loginLockoutService;
        }

        // Метод для проверки пароля
        public bool AttemptPassword(User user, string password, bool accountLocked)
        {
            string hashToUse = (user == null || accountLocked) ? DummyHash : user.PasswordHash;
            string saltToUse = (user == null || accountLocked) ? DummySalt : user.PasswordSalt;

            // Проверяем пароль, используя реальные или фиктивные данные
            bool passwordCorrect = _passwordHasher.Verify(password, hashToUse, saltToUse);

            if (accountLocked || user == null)
                return false;

            if (!passwordCorrect)
            {
                _loginLockoutService.PasswordIncorrect(user);
                return false;
            }

            LoginLockoutService.ErrorReset(user);

            return true;
        }
    }
}