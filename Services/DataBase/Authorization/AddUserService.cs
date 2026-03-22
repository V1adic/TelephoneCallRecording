using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.DataBase.Authorization
{
    public class AddUserService
    {
        public static async Task<bool> AddUser(AppDbContext _db, string username, string passwordHash, string passwordSalt, string email, string codeHash)
        {
            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                IsEmailConfirmed = false,
                EmailConfirmationCodeHash = codeHash,
                EmailConfirmationExpires = DateTime.UtcNow.AddMinutes(10)
            };
            _db.Users.Add(user);

            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
