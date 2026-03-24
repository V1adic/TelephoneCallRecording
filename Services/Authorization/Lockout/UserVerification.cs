using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Lockout
{
    public class UserVerification
    {
        static public bool Check(User user)
        {
            return true; // TODO: Добавить сложную проверку на необходимость прохождения 2FA на основе логов.
        }

        static async public Task Verification(User user)
        {
            (string codeHash, string code) = CodeFactory.GetCodeHash();
            user.IsEmailConfirmed = false;
            user.EmailConfirmationCodeHash = codeHash;
            await EmailService.SendConfirmationCodeAsync(user.Email, code);
        }
    }
}
