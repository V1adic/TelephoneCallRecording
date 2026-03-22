using TelephoneCallRecording.Models.Authorization;
using Microsoft.EntityFrameworkCore;

namespace TelephoneCallRecording.Services.DataBase.Authorization
{
    public class FindUserService
    {
        static public async Task<User?> SearchByUsername(AppDbContext _db, string username)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.Username == username);

            return user;
        }

        static public async Task<bool> ContainsUser(AppDbContext _db, string username)
        {
            return await _db.Users.AnyAsync(x => x.Username == username);
        }
    }
}
