using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Login;
using TelephoneCallRecording.Services.Cryptography.Authorization;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Register
{
    public class RegisterService
    {
        static public async Task<bool> AttemptRegister(AppDbContext _db, string username, string password, string email)
        {
            var requestValidation = RegisterRequestVerifire.IsValid(username, password, email);

            if (!string.IsNullOrEmpty(requestValidation)) // Через фронтенд передать о конкретной ошибке.
            {
                return false;
            }

            var (hash, salt) = PasswordHasher.HashPassword(password);
            (string codeHash, string code) = CodeFactory.GetCodeHash();

            if (await FindUserService.ContainsUser(_db, username))
                return false;

            if (await AddUserService.AddUser(_db, username, hash, salt, email, codeHash))
            {
                await EmailService.SendConfirmationCodeAsync(email, code);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
