using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace TelephoneCallRecording.Services.Cryptography.Authorization
{
    public interface IPasswordHasher
    {
        (string hash, string salt) HashPassword(string password);
        bool Verify(string password, string storedHash, string storedSalt);
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;           // байт
        private const int HashSize = 32;           // байт (256 бит)
        private const int MemorySize = 65536;      // KiB = 64 MiB
        private const int Iterations = 3;
        private const int DegreeOfParallelism = 1;

        public (string hash, string salt) HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = saltBytes,
                DegreeOfParallelism = DegreeOfParallelism,
                MemorySize = MemorySize,
                Iterations = Iterations
            };

            byte[] hashBytes = argon2.GetBytes(HashSize);

            return (
                Convert.ToBase64String(hashBytes),
                Convert.ToBase64String(saltBytes)
            );
        }

        public bool Verify(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);

            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = saltBytes,
                DegreeOfParallelism = DegreeOfParallelism,
                MemorySize = MemorySize,
                Iterations = Iterations
            };

            byte[] computedHash = argon2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
        }
    }
}