using System.Text.RegularExpressions;
using TelephoneCallRecording.Services.Authorization.Email;

namespace TelephoneCallRecording.Services.Authorization.Register
{
    public partial class RegisterRequestVerifire
    {
        private const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?])[A-Za-z\d@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?]{12,}$";

        [GeneratedRegex(PasswordRegex)]
        private static partial Regex GetPasswordRegex();

        public static string IsValid(string username, string password, string email)
        {
            var emailError = IsValidEmail(email);
            if (!string.IsNullOrEmpty(emailError))
                return emailError;
            var usernameError = IsValidUsername(username);
            if (!string.IsNullOrEmpty(usernameError))
                return usernameError;
            var passwordError = IsValidPassword(password);
            if (!string.IsNullOrEmpty(passwordError))
                return passwordError;
            return string.Empty;
        }

        private static string IsValidEmail(string email)
        {
            if (!EmailService.IsValidEmail(email))
                return "Email error";
            else
                return string.Empty;
        }
        private static string IsValidUsername(string username)
        {
            var trimmedUsername = username.Trim();
            if (trimmedUsername.Length > 15 ||
                trimmedUsername.Length < 5 ||
                string.IsNullOrEmpty(trimmedUsername) ||
                trimmedUsername.Contains(' '))
                return "Username error";
            else
                return string.Empty;
        }
        private static string IsValidPassword(string password)
        {
            if (!GetPasswordRegex().IsMatch(password))
                return "Password error";
            else
                return string.Empty;
        }
    }
}