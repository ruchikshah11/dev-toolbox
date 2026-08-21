using System.Security.Cryptography;
using System.Text;

namespace DevToolbox.Tools.HmacGenerator
{
    public static class HmacGeneratorService
    {
        public static readonly string[] Algorithms = { "HMACMD5", "HMACSHA1", "HMACSHA256", "HMACSHA512" };

        // A shared CSPRNG instance (like PasswordGeneratorService's Rng) rather than System.Random
        // - a secret HMAC key is exactly the kind of value that must not be predictable.
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        // Each algorithm's own ideal key length (matching its hash output size, the standard
        // recommendation for HMAC keys) rather than one fixed size for all four - HMACSHA256
        // wants a 32-byte key exactly (what `openssl rand -hex 32` produces), but that would be
        // under-strength for HMACSHA512 and needlessly long for HMACMD5.
        public static int KeyByteLengthFor(string algorithm) => algorithm switch
        {
            "HMACMD5" => 16,
            "HMACSHA1" => 20,
            "HMACSHA256" => 32,
            "HMACSHA512" => 64,
            _ => 32
        };

        /// <summary>Generates a cryptographically random key as lowercase hex - the same output format as `openssl rand -hex &lt;byteLength&gt;` (e.g. 32 bytes -> 64 lowercase hex characters, no separators).</summary>
        public static string GenerateRandomKeyHex(int byteLength)
        {
            var bytes = new byte[byteLength];
            Rng.GetBytes(bytes);
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }

        public static string Compute(string message, string secretKey, string algorithm)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey ?? string.Empty);
            var messageBytes = Encoding.UTF8.GetBytes(message ?? string.Empty);

            using HMAC hmac = algorithm switch
            {
                "HMACMD5" => new HMACMD5(keyBytes),
                "HMACSHA1" => new HMACSHA1(keyBytes),
                "HMACSHA256" => new HMACSHA256(keyBytes),
                "HMACSHA512" => new HMACSHA512(keyBytes),
                _ => throw new ArgumentException($"Unknown algorithm '{algorithm}'.", nameof(algorithm))
            };

            var hash = hmac.ComputeHash(messageBytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
