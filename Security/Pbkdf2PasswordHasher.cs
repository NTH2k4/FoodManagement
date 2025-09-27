using FoodManagement.Contracts;
using System.Security.Cryptography;

namespace FoodManagement.Security
{
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public (string hashBase64, string saltBase64) HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);
            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = derive.GetBytes(HashSize);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public bool VerifyPassword(string password, string hashBase64, string saltBase64)
        {
            var salt = Convert.FromBase64String(saltBase64);
            var expected = Convert.FromBase64String(hashBase64);
            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var test = derive.GetBytes(expected.Length);
            return CryptographicOperations.FixedTimeEquals(test, expected);
        }
    }
}
