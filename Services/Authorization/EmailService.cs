using System.Net.Mail;

namespace TelephoneCallRecording.Services.Authorization
{
    public class EmailService
    {
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
            // Здесь должна быть реальная реализация отправки email
            // Например, с помощью SMTP-клиента или стороннего сервиса
            Console.WriteLine($"Отправка кода {code} на email {email}");
        }
    }
}
