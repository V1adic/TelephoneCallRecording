using System.Text.RegularExpressions;
using TelephoneCallRecording.Services.Authorization.Email;

namespace TelephoneCallRecording.Services.Authorization.Register
{
    public partial class RegisterRequestVerifier
    {
        private const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?])[A-Za-z\d@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?]{12,}$";
        private const string PhoneRegex = @"^\+\d{11,15}$";
        private const string InnRegex = @"^\d{12}$";

        [GeneratedRegex(PasswordRegex)]
        private static partial Regex GetPasswordRegex();

        [GeneratedRegex(PhoneRegex)]
        private static partial Regex GetPhoneRegex();

        [GeneratedRegex(InnRegex)]
        private static partial Regex GetInnRegex();

        public static string IsValid(
            string username,
            string password,
            string email,
            string phoneNumber,
            string inn,
            string address)
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

            var phoneError = IsValidPhone(phoneNumber);
            if (!string.IsNullOrEmpty(phoneError))
                return phoneError;

            var innError = IsValidInn(inn);
            if (!string.IsNullOrEmpty(innError))
                return innError;

            var addressError = IsValidAddress(address);
            if (!string.IsNullOrEmpty(addressError))
                return addressError;

            return string.Empty;
        }

        private static string IsValidEmail(string email)
        {
            return !EmailService.IsValidEmail(email) ? "Email error" : string.Empty;
        }

        private static string IsValidUsername(string username)
        {
            var trimmedUsername = username.Trim();
            if (trimmedUsername.Length > 15 ||
                trimmedUsername.Length < 5 ||
                string.IsNullOrEmpty(trimmedUsername) ||
                trimmedUsername.Contains(' '))
                return "Username error";

            return string.Empty;
        }

        private static string IsValidPassword(string password)
        {
            return !GetPasswordRegex().IsMatch(password) ? "Password error" : string.Empty;
        }

        private static string IsValidPhone(string phoneNumber)
        {
            return !GetPhoneRegex().IsMatch(phoneNumber) ? "Phone error" : string.Empty;
        }

        private static string IsValidInn(string inn)
        {
            return !GetInnRegex().IsMatch(inn) ? "INN error" : string.Empty;
        }

        private static string IsValidAddress(string address)
        {
            return string.IsNullOrWhiteSpace(address) || address.Trim().Length is < 5 or > 250
                ? "Address error"
                : string.Empty;
        }
    }
}
