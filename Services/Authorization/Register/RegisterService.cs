using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;
using TelephoneCallRecording.Services.Authorization.Register;
using TelephoneCallRecording.Services.Cryptography.Authorization;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Register
{
    public interface IRegisterService
    {
        Task<bool> AttemptRegister(string username, string password, string email);
    }

    public class RegisterService : IRegisterService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfirmationCodeGenerator _codeGenerator;
        private readonly IEmailService _emailService;
        private readonly IOptions<Lockout.Options.LockoutOptions> _emailOptions;

        public RegisterService(IUserRepository userRepository, IPasswordHasher passwordHasher, IConfirmationCodeGenerator codeGenerator, IEmailService emailService, IOptions<Lockout.Options.LockoutOptions> emailOptions)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _codeGenerator = codeGenerator;
            _emailService = emailService;
            _emailOptions = emailOptions;
        }

        public async Task<bool> AttemptRegister(string username, string password, string email)
        {
            var requestValidation = RegisterRequestVerifier.IsValid(username, password, email);
            if (!string.IsNullOrEmpty(requestValidation))
                return false;

            var (hash, salt) = _passwordHasher.HashPassword(password);
            (string codeHash, string code) = _codeGenerator.Generate();

            await using var transaction = await _userRepository.BeginTransactionAsync();

            try
            {
                if (await _userRepository.ExistsByUsernameAsync(username))
                {
                    await _userRepository.RollbackTransactionAsync(transaction);
                    return false;
                }

                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    IsEmailConfirmed = false,
                    EmailConfirmationCodeHash = codeHash,
                    EmailConfirmationExpires = DateTime.UtcNow.AddMinutes(_emailOptions.Value.CodeExpirationMinutes)
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                await _userRepository.CommitTransactionAsync(transaction);

                await _emailService.SendConfirmationCodeAsync(email, code);
                return true;
            }
            catch (Exception)
            {
                await _userRepository.RollbackTransactionAsync(transaction);
                return false;
            }
        }
    }
}