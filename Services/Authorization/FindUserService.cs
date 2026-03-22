using TelephoneCallRecording.Models.Authorization;
using Microsoft.EntityFrameworkCore;

namespace TelephoneCallRecording.Services.Authorization
{
    public class FindUserService
    {
        static public async Task<User?> SearchByUsername(AppDbContext _db, string username)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.Username == username);

            return user;
        }
    }
}
