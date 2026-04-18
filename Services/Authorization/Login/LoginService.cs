using System.Security.Claims;
using TelephoneCallRecording.Services.Authorization.Lockout;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public sealed record LoginAttemptResult(
        bool Success,
        ClaimsPrincipal? Principal,
        string Code,
        string Message);

    public interface ILoginService
    {
        Task<LoginAttemptResult> AttemptLogin(string username, string password);
    }

    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordValidator _passwordValidator;

        public LoginService(IUserRepository userRepository, IPasswordValidator passwordValidator)
        {
            _userRepository = userRepository;
            _passwordValidator = passwordValidator;
        }

        public async Task<LoginAttemptResult> AttemptLogin(string username, string password)
        {
            await using var transaction = await _userRepository.BeginTransactionAsync();

            try
            {
                var user = await _userRepository.FindByUsernameAsync(username);
                var confirmedUser = user is { IsEmailConfirmed: true } ? user : null;
                var accountLocked = confirmedUser is not null && LoginLockoutService.AttemptLockout(confirmedUser);
                var passwordValid = _passwordValidator.AttemptPassword(confirmedUser, password, accountLocked);

                await _userRepository.SaveChangesAsync();

                if (confirmedUser == null)
                {
                    await _userRepository.RollbackTransactionAsync(transaction);
                    return new LoginAttemptResult(false, null, "invalid_credentials", "Неверные учётные данные.");
                }

                if (accountLocked)
                {
                    await _userRepository.CommitTransactionAsync(transaction);
                    return new LoginAttemptResult(false, null, "locked", "Аккаунт временно заблокирован после неудачных попыток.");
                }

                if (!passwordValid)
                {
                    await _userRepository.CommitTransactionAsync(transaction);
                    return new LoginAttemptResult(false, null, "invalid_credentials", "Неверные учётные данные.");
                }

                await _userRepository.CommitTransactionAsync(transaction);

                return new LoginAttemptResult(
                    true,
                    ClaimsPrincipalFactory.Get(confirmedUser),
                    "ok",
                    "Вход выполнен.");
            }
            catch
            {
                await _userRepository.RollbackTransactionAsync(transaction);
                return new LoginAttemptResult(false, null, "server_error", "Не удалось выполнить вход.");
            }
        }
    }
}
