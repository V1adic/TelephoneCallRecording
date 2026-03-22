using System.Net.Mail;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public class EmailService
    {
        public static async Task<bool> AttemptEmailConfirmationAsync(AppDbContext _db, string username, string code)
        {
            string incomingCodeHash = CodeFactory.GetHash(code);
            var user = await FindUserService.SearchByUsername(_db, username);

            bool isCodeValid = ErrorEmailConfirmation.IsValidCode(user, incomingCodeHash);

            if (isCodeValid && user != null)
            {
                user.IsEmailConfirmed = true;
                user.EmailConfirmationCodeHash = null;
                user.EmailConfirmationExpires = null;
                user.FailedEmailConfirmAttempts = 0;
                user.LockoutEnd = null;

                await _db.SaveChangesAsync();

                return true;
            }
            else
            {
                await _db.SaveChangesAsync();
                return false;
            }
        }

        public static bool IsValidEmail(string email)
        {
            if (MailAddress.TryCreate(email, out var addr))
            {
                return addr.Address == email;
            }
            return false;
        }

        public static async Task SendConfirmationCodeAsync(string email, string code)
        {
            /*      TODO        */
            // Здесь должна быть реальная реализация отправки email
            // Например, с помощью SMTP-клиента или стороннего сервиса
            Console.WriteLine($"Отправка кода {code} на email {email}");
        }
    }
}
