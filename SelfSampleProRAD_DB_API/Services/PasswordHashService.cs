using System.Security.Cryptography;
using System.Text;

namespace SelfSampleProRAD_DB_API.Services
{
    public class PasswordHashService
    {
        // This is a simple implementation using SHA256 with a salt
        // For production, consider using a more robust algorithm like BCrypt or PBKDF2

        private readonly string _pepper; // Additional server-side secret
        private const int SaltSize = 16; // 128 bits

        public PasswordHashService(IConfiguration configuration)
        {
            _pepper = configuration["PasswordSecurity:Pepper"] ?? "DefaultPepperValue12345";
        }

        public string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = GenerateSalt();
            
            // Combine password with salt and pepper
            string saltedPepperedPass = password + Convert.ToBase64String(salt) + _pepper;
            
            // Hash the combined string
            byte[] hashBytes = ComputeHash(saltedPepperedPass);
            
            // Combine salt and hash for storage (salt:hash)
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hashBytes)}";
        }

        public bool VerifyPassword(string providedPassword, string storedHash)
        {
            // Extract salt and hash from stored value
            string[] parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            string storedSaltStr = parts[0];
            string storedHashStr = parts[1];

            byte[] storedSalt = Convert.FromBase64String(storedSaltStr);
            byte[] storedHashBytes = Convert.FromBase64String(storedHashStr);

            // Recreate the hash using the provided password and stored salt
            string saltedPepperedPass = providedPassword + storedSaltStr + _pepper;
            byte[] computedHashBytes = ComputeHash(saltedPepperedPass);

            // Compare the computed hash with the stored hash
            return CompareByteArrays(computedHashBytes, storedHashBytes);
        }

        private byte[] GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] salt = new byte[SaltSize];
                rng.GetBytes(salt);
                return salt;
            }
        }

        private byte[] ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            // Use a constant-time comparison to prevent timing attacks
            int result = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                result |= array1[i] ^ array2[i];
            }

            return result == 0;
        }
    }
}
