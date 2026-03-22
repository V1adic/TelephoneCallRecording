using System.Security.Cryptography;
using System.Text;

namespace TelephoneCallRecording.Services.Authorization.Email
{
    public class CodeFactory
    {
        public static (string, string) GetCodeHash()
        {
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var codeHash = GetHash(code);
            return (codeHash, code);
        }

        public static string GetHash(string code)
        {
            var bytes = Encoding.UTF8.GetBytes(code);
            string codeHash = Convert.ToBase64String(SHA256.HashData(bytes));
            return codeHash;
        }
    }
}
