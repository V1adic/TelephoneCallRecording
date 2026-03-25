using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.DataBase.Authorization
{
    public interface IUserRepository
    {
        Task<User?> FindByUsernameAsync(string username);
        Task<bool> ExistsByUsernameAsync(string username);
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

        public async Task<User?> FindByUsernameAsync(string username)
        {
            return await _db.Users
                .FirstOrDefaultAsync(x => x.Username == username);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _db.Users.AnyAsync(x => x.Username == username);
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
            if (transaction != null)
                await transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync(IDbContextTransaction transaction)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
        }
    }
}