using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.DataBase.Authorization
{
    public interface IUserRepository
    {
        Task<User?> FindByIdAsync(int userId);
        Task<User?> FindByUsernameAsync(string username);
        Task<bool> ExistsByUsernameAsync(string username);
        Task<bool> ExistsByEmailAsync(string email);
        Task AddAsync(User user);
        Task SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync(IDbContextTransaction transaction);
        Task RollbackTransactionAsync(IDbContextTransaction transaction);
    }

    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<User?> FindByIdAsync(int userId)
        {
            return await _db.Users
                .Include(x => x.Subscriber)
                .ThenInclude(x => x!.City)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<User?> FindByUsernameAsync(string username)
        {
            return await _db.Users
                .Include(x => x.Subscriber)
                .ThenInclude(x => x!.City)
                .FirstOrDefaultAsync(x => x.Username == username);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _db.Users.AnyAsync(x => x.Username == username);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _db.Users.AnyAsync(x => x.Email == email);
        }

        public Task AddAsync(User user)
        {
            _db.Users.Add(user);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _db.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync(IDbContextTransaction transaction)
        {
            await transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync(IDbContextTransaction transaction)
        {
            await transaction.RollbackAsync();
        }
    }
}
