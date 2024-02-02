using System.Security.Cryptography;
using System.Text;

namespace Process_Software
{
    public class HashingHelpers
    {
        private const int saltSize = 32;
        private const int keySize = 64;
        private const int iterations = 350000;
        private static readonly HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA512;

        public static string HashPassword(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] saltBytes = new byte[saltSize];
                rng.GetBytes(saltBytes);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, hashAlgorithm))
                {
                    byte[] keyBytes = pbkdf2.GetBytes(keySize);
                    byte[] resultBytes = new byte[saltSize + keySize];
                    Buffer.BlockCopy(saltBytes, 0, resultBytes, 0, saltSize);
                    Buffer.BlockCopy(keyBytes, 0, resultBytes, saltSize, keySize);

                    return BitConverter.ToString(resultBytes).Replace("-", "").ToLower();
                }
            }
        }
        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] hashedBytes = Enumerable.Range(0, hashedPassword.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hashedPassword.Substring(x, 2), 16))
                .ToArray();

            byte[] saltBytes = new byte[saltSize];
            Buffer.BlockCopy(hashedBytes, 0, saltBytes, 0, saltSize);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, hashAlgorithm))
            {
                byte[] keyBytes = pbkdf2.GetBytes(keySize);

                for (int i = 0; i < keySize; i++)
                {
                    if (hashedBytes[i + saltSize] != keyBytes[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
