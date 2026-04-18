using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.Authorization.Lockout
{
    public interface IUserVerificationService
    {
        Task TriggerEmailVerificationAsync(User user);
        bool RequiresVerification(User user);
    }

    public class UserVerificationService : IUserVerificationService
    {
        public Task TriggerEmailVerificationAsync(User user)
        {
            return Task.CompletedTask;
        }

        public bool RequiresVerification(User user)
        {
            return false;
        }
    }
}
