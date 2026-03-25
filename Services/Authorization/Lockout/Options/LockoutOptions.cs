namespace TelephoneCallRecording.Services.Authorization.Lockout.Options
{
    public class LockoutOptions
    {
        public const string SectionName = "EmailConfirmation";

        public int MaxFailedLoginAttempts { get; set; } = 5;

        public int CodeExpirationMinutes { get; set; } = 10;

        public int MaxFailedConfirmAttempts { get; set; } = 5;

        public int LockoutDurationMinutes { get; set; } = 15;
    }
}
