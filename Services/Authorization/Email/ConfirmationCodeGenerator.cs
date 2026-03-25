using System.Security.Cryptography;
using System.Text;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public interface IConfirmationCodeGenerator
    {
        (string CodeHash, string Code) Generate();
        string GetHash(string code);
    }

    public class ConfirmationCodeGenerator : IConfirmationCodeGenerator
    {
        public (string CodeHash, string Code) Generate()
        {
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var codeHash = GetHash(code);
            return (codeHash, code);
        }

        public string GetHash(string code)
        {
            var bytes = Encoding.UTF8.GetBytes(code);
            string codeHash = Convert.ToBase64String(SHA256.HashData(bytes));
            return codeHash;
        }
    }
}