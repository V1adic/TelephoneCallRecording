namespace TelephoneCallRecording.Services.Authorization.Email
{
    public class EmailDeliveryOptions
    {
        public const string SectionName = "EmailDelivery";

        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        public bool UseSsl { get; set; } = true;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = "TelephoneCallRecording";

        public bool LogCodesInDevelopment { get; set; } = true;
    }
}
