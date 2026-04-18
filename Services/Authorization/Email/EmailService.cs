using System.Net.Mail;
using Microsoft.Extensions.Options;
using TelephoneCallRecording.Services.Authorization.Lockout;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public sealed record EmailOperationResult(bool Success, string Code, string Message);

    public interface IEmailService
    {
        Task<EmailOperationResult> AttemptEmailConfirmationAsync(string username, string code);
        Task<EmailOperationResult> RequestConfirmationCodeAsync(string username);
        bool IsValidEmail(string email);
        Task<bool> SendConfirmationCodeAsync(string email, string code);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailDeliveryOptions _options;
        private readonly LockoutOptions _lockoutOptions;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserRepository _userRepository;
        private readonly IConfirmationCodeGenerator _codeGenerator;
        private readonly IEmailConfirmationValidator _emailConfirmationValidator;

        public EmailService(
            ILogger<EmailService> logger,
            IOptions<EmailDeliveryOptions> options,
            IOptions<LockoutOptions> lockoutOptions,
            IWebHostEnvironment environment,
            IUserRepository userRepository,
            IConfirmationCodeGenerator codeGenerator,
            IEmailConfirmationValidator emailConfirmationValidator)
        {
            _logger = logger;
            _options = options.Value;
            _lockoutOptions = lockoutOptions.Value;
            _environment = environment;
            _userRepository = userRepository;
            _codeGenerator = codeGenerator;
            _emailConfirmationValidator = emailConfirmationValidator;
        }

        public static bool IsValidEmail(string email)
        {
            if (MailAddress.TryCreate(email, out var addr))
            {
                return addr.Address == email;
            }

            return false;
        }

        bool IEmailService.IsValidEmail(string email) => IsValidEmail(email);

        public async Task<EmailOperationResult> AttemptEmailConfirmationAsync(string username, string code)
        {
            var incomingCodeHash = _codeGenerator.GetHash(code);
            var user = await _userRepository.FindByUsernameAsync(username);

            var isCodeValid = _emailConfirmationValidator.IsValidCode(user, incomingCodeHash);
            if (isCodeValid && user != null)
            {
                user.IsEmailConfirmed = true;
                user.EmailConfirmationCodeHash = null;
                user.EmailConfirmationExpires = null;
                user.FailedEmailConfirmAttempts = 0;
                user.LockoutEnd = null;

                await _userRepository.SaveChangesAsync();
                return new EmailOperationResult(true, "confirmed", "Email подтверждён.");
            }

            await _userRepository.SaveChangesAsync();
            return new EmailOperationResult(false, "invalid_code", "Код подтверждения недействителен или истёк.");
        }

        public async Task<EmailOperationResult> RequestConfirmationCodeAsync(string username)
        {
            var user = await _userRepository.FindByUsernameAsync(username);
            if (user == null)
            {
                return new EmailOperationResult(false, "verification_missing", "Сессия подтверждения истекла.");
            }

            if (user.IsEmailConfirmed)
            {
                return new EmailOperationResult(false, "already_confirmed", "Email уже подтверждён.");
            }

            if (EmailLockoutService.AttemptLockout(user))
            {
                await _userRepository.SaveChangesAsync();
                return new EmailOperationResult(false, "locked", "Слишком много попыток. Повторите позже.");
            }

            var (codeHash, code) = _codeGenerator.Generate();
            user.EmailConfirmationCodeHash = codeHash;
            user.EmailConfirmationExpires = DateTime.UtcNow.AddMinutes(_lockoutOptions.CodeExpirationMinutes);
            user.FailedEmailConfirmAttempts = 0;
            user.LockoutEnd = null;

            await _userRepository.SaveChangesAsync();
            var delivered = await SendConfirmationCodeAsync(user.Email, code);
            return delivered
                ? new EmailOperationResult(true, "resent", "Новый код отправлен.")
                : new EmailOperationResult(true, "resent_delivery_failed", "Код обновлён, но письмо не отправлено. Проверьте настройки SMTP.");
        }

        public async Task<bool> SendConfirmationCodeAsync(string email, string code)
        {
            if (_environment.IsDevelopment() && _options.LogCodesInDevelopment)
            {
                Console.WriteLine($"DEV confirmation code for {email}: {code}");
            }

            if (string.IsNullOrWhiteSpace(_options.SmtpHost) ||
                string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                _logger.LogWarning("SMTP is not configured. Confirmation code delivery skipped for {Email}.", email);
                return _environment.IsDevelopment() && _options.LogCodesInDevelopment;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = "TelephoneCallRecording email confirmation",
                Body = $"Ваш код подтверждения: {code}\nКод действует 15 минут.",
                IsBodyHtml = false
            };
            message.To.Add(email);

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new System.Net.NetworkCredential(_options.Username, _options.Password);
            }

            try
            {
                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email}.", email);
                return false;
            }
        }
    }
}
