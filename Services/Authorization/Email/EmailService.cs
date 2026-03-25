using System.Net.Mail;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public interface IEmailService
    {
        Task<bool> AttemptEmailConfirmationAsync(string username, string code);
        bool IsValidEmail(string email);
        Task SendConfirmationCodeAsync(string email, string code);
    }

    public class EmailService : IEmailService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfirmationCodeGenerator _codeGenerator;
        private readonly IEmailConfirmationValidator _emailConfirmationValidator;

        public EmailService(IUserRepository userRepository, IConfirmationCodeGenerator codeGenerator, IEmailConfirmationValidator emailConfirmationValidator)
        {
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

        public async Task<bool> AttemptEmailConfirmationAsync(string username, string code)
        {
            string incomingCodeHash = _codeGenerator.GetHash(code);
            var user = await _userRepository.FindByUsernameAsync(username);

            bool isCodeValid = _emailConfirmationValidator.IsValidCode(user, incomingCodeHash);

            if (isCodeValid && user != null)
            {
                user.IsEmailConfirmed = true;
                user.EmailConfirmationCodeHash = null;
                user.EmailConfirmationExpires = null;
                user.FailedEmailConfirmAttempts = 0;
                user.LockoutEnd = null;

                await _userRepository.SaveChangesAsync();

                return true;
            }
            else
            {
                await _userRepository.SaveChangesAsync();
                return false;
            }
        }

        public async Task SendConfirmationCodeAsync(string email, string code)
        {
            /*      TODO        */
            // Здесь должна быть реальная реализация отправки email
            // Например, с помощью SMTP-клиента или стороннего сервиса
            Console.WriteLine($"Отправка кода {code} на email {email}");
        }
    }
}