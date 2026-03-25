using System.Security.Claims;
using TelephoneCallRecording.Services.Authorization.Lockout;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Login
{
    public interface ILoginService
    {
        Task<ClaimsPrincipal?> AttemptLogin(string username, string password);
    }

    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordValidator _passwordValidator;
        private readonly IUserVerificationService _userVerificationService;

        public LoginService(IUserRepository userRepository, IPasswordValidator passwordValidator, IUserVerificationService userVerificationService)
        {
            _userRepository = userRepository;
            _passwordValidator = passwordValidator;
            _userVerificationService = userVerificationService;
        }

        public async Task<ClaimsPrincipal?> AttemptLogin(string username, string password)
        {
            await using var transaction = await _userRepository.BeginTransactionAsync();

            try
            {
                var user = await _userRepository.FindByUsernameAsync(username);

                if (user == null)
                {
                    await _userRepository.RollbackTransactionAsync(transaction);
                    return null;
                }
                if (!user.IsEmailConfirmed)
                {
                    await _userRepository.RollbackTransactionAsync(transaction);
                    return null;
                }

                bool accountLocked = LoginLockoutService.AttemptLockout(user);
                bool passwordValid = _passwordValidator.AttemptPassword(user, password, accountLocked);

                if (_userVerificationService.RequiresVerification(user))
                {
                    await _userVerificationService.TriggerEmailVerificationAsync(user);
                }

                await _userRepository.SaveChangesAsync();
                await _userRepository.CommitTransactionAsync(transaction);

                return passwordValid ? ClaimsPrincipalFactory.Get(user) : null;
            }
            catch (Exception)
            {
                await _userRepository.RollbackTransactionAsync(transaction);
                return null;
            }
        }
    }
}
