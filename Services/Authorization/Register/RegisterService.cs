using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Models.Calls;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;
using TelephoneCallRecording.Services.Cryptography.Authorization;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Register
{
    public sealed record RegisterAttemptResult(
        bool Success,
        string Code,
        string Message,
        User? User = null);

    public interface IRegisterService
    {
        Task<RegisterAttemptResult> AttemptRegister(
            string username,
            string password,
            string email,
            string phoneNumber,
            string inn,
            string address,
            int cityId);
    }

    public class RegisterService : IRegisterService
    {
        private readonly AppDbContext _db;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfirmationCodeGenerator _codeGenerator;
        private readonly IEmailService _emailService;
        private readonly IOptions<LockoutOptions> _emailOptions;
        private readonly ILogger<RegisterService> _logger;

        public RegisterService(
            AppDbContext db,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IConfirmationCodeGenerator codeGenerator,
            IEmailService emailService,
            IOptions<LockoutOptions> emailOptions,
            ILogger<RegisterService> logger)
        {
            _db = db;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _codeGenerator = codeGenerator;
            _emailService = emailService;
            _emailOptions = emailOptions;
            _logger = logger;
        }

        public async Task<RegisterAttemptResult> AttemptRegister(
            string username,
            string password,
            string email,
            string phoneNumber,
            string inn,
            string address,
            int cityId)
        {
            var requestValidation = RegisterRequestVerifier.IsValid(
                username,
                password,
                email,
                phoneNumber,
                inn,
                address);
            if (!string.IsNullOrEmpty(requestValidation))
                return new RegisterAttemptResult(false, "invalid_request", "Проверьте введённые данные.");

            if (!await _db.Cities.AnyAsync(x => x.Id == cityId))
                return new RegisterAttemptResult(false, "city_not_found", "Выбранный город недоступен.");

            if (await _userRepository.ExistsByUsernameAsync(username))
                return new RegisterAttemptResult(false, "duplicate_username", "Пользователь с таким именем уже существует.");

            if (await _userRepository.ExistsByEmailAsync(email))
                return new RegisterAttemptResult(false, "duplicate_email", "Пользователь с таким email уже существует.");

            if (await _db.Subscribers.AnyAsync(x => x.PhoneNumber == phoneNumber))
                return new RegisterAttemptResult(false, "duplicate_phone", "Абонент с таким номером уже зарегистрирован.");

            var (hash, salt) = _passwordHasher.HashPassword(password);
            var (codeHash, code) = _codeGenerator.Generate();

            await using var transaction = await _userRepository.BeginTransactionAsync();

            try
            {
                var subscriber = new Subscriber
                {
                    PhoneNumber = phoneNumber,
                    Inn = inn,
                    Address = address,
                    CityId = cityId
                };

                await _db.Subscribers.AddAsync(subscriber);
                await _db.SaveChangesAsync();

                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    IsEmailConfirmed = false,
                    EmailConfirmationCodeHash = codeHash,
                    EmailConfirmationExpires = DateTime.UtcNow.AddMinutes(_emailOptions.Value.CodeExpirationMinutes),
                    SubscriberId = subscriber.Id,
                    Subscriber = subscriber,
                    Role = "Client"
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
                await _userRepository.CommitTransactionAsync(transaction);

                var delivered = await _emailService.SendConfirmationCodeAsync(email, code);
                return delivered
                    ? new RegisterAttemptResult(true, "registered", "Аккаунт создан. Подтвердите email кодом из письма.", user)
                    : new RegisterAttemptResult(true, "registered_delivery_failed", "Аккаунт создан, но письмо не отправлено. Проверьте настройки SMTP или запросите код повторно.", user);
            }
            catch
            {
                _logger.LogError("Registration transaction failed for username {Username}.", username);
                await _userRepository.RollbackTransactionAsync(transaction);
                return new RegisterAttemptResult(false, "server_error", "Не удалось завершить регистрацию.");
            }
        }
    }
}
