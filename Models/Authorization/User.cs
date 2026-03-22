namespace TelephoneCallRecording.Models.Authorization
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string PasswordSalt { get; set; } = null!;

        public bool IsEmailConfirmed { get; set; }

        public string? EmailConfirmationCodeHash { get; set; }

        public DateTime? EmailConfirmationExpires { get; set; }

        public int FailedLoginAttempts { get; set; }

        public int FailedEmailConfirmAttempts { get; set; }

        public DateTime? LockoutEnd { get; set; }
    }
}
