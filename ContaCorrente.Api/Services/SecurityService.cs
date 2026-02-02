using System.Security.Cryptography;

namespace BancoDigitalAna.ContaCorrente.Api.Services
{
    public class SecurityService : ISecurityService
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public (string Hash, string Salt) HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var rfc = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = rfc.GetBytes(KeySize);

            return (Convert.ToBase64String(key), Convert.ToBase64String(salt));
        }

        public bool VerifyPassword(string password, string hashBase64, string saltBase64)
        {
            var salt = Convert.FromBase64String(saltBase64);
            using var rfc = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = rfc.GetBytes(KeySize);
            var computed = Convert.ToBase64String(key);
            return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(computed), Convert.FromBase64String(hashBase64));
        }
    }
}
