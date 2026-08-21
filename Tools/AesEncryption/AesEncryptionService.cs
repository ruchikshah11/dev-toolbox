using System.Security.Cryptography;
using System.Text;

namespace DevToolbox.Tools.AesEncryption
{
    /// <summary>
    /// Password-based AES-256-GCM encrypt/decrypt for arbitrary text, in the same spirit as
    /// online "AES encrypt/decrypt" tools: the user supplies a plain password, not a raw key/IV
    /// to manage themselves. AES-GCM (not CBC) is used because it's authenticated - decrypting
    /// with the wrong password or against tampered/corrupted ciphertext fails loudly (a
    /// CryptographicException from the tag check) rather than silently producing garbage
    /// plaintext, which plain CBC alone can't detect. AesGcm is only available on modern .NET
    /// (added in .NET Core 3.0), not the .NET Framework 4.7.2 this app originally targeted - one
    /// of the concrete benefits unlocked by the net10.0 migration.
    ///
    /// Output format is a single Base64 string, so there's nothing else for the user to copy
    /// around: Base64( salt(16) || nonce(12) || tag(16) || ciphertext ). The salt and nonce are
    /// fresh random values per encryption (never reused), embedded alongside the ciphertext so
    /// decryption only needs the password back, not any other value memorized separately.
    /// </summary>
    public static class AesEncryptionService
    {
        private const int SaltSize = 16;
        private const int NonceSize = 12; // AES-GCM's standard/recommended nonce size
        private const int TagSize = 16;
        private const int KeySize = 32; // AES-256

        // OWASP's current (2023) minimum recommendation for PBKDF2-HMAC-SHA256 - deliberately
        // expensive (on the order of ~100-300ms per call) so a stolen ciphertext resists offline
        // password guessing; still fine for a UI button click, not a hot loop.
        private const int Pbkdf2Iterations = 210_000;

        /// <summary>Encrypts <paramref name="plaintext"/> with <paramref name="password"/>, returning a self-contained Base64 blob (fresh random salt/nonce embedded).</summary>
        public static string Encrypt(string plaintext, string password)
        {
            if (string.IsNullOrEmpty(password)) throw new FormatException("Enter a password to encrypt with.");

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var key = DeriveKey(password, salt);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext ?? string.Empty);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSize];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            }

            var blob = new byte[SaltSize + NonceSize + TagSize + ciphertext.Length];
            var offset = 0;
            Buffer.BlockCopy(salt, 0, blob, offset, SaltSize); offset += SaltSize;
            Buffer.BlockCopy(nonce, 0, blob, offset, NonceSize); offset += NonceSize;
            Buffer.BlockCopy(tag, 0, blob, offset, TagSize); offset += TagSize;
            Buffer.BlockCopy(ciphertext, 0, blob, offset, ciphertext.Length);

            return Convert.ToBase64String(blob);
        }

        /// <summary>Decrypts a Base64 blob produced by <see cref="Encrypt"/> using <paramref name="password"/>. Throws a FormatException with a clear message for a malformed blob, or a CryptographicException (wrong password/tampered data) - see the wrapping in the UI layer for how these are shown.</summary>
        public static string Decrypt(string base64Blob, string password)
        {
            if (string.IsNullOrEmpty(password)) throw new FormatException("Enter the password it was encrypted with.");
            if (string.IsNullOrWhiteSpace(base64Blob)) throw new FormatException("Paste the encrypted Base64 text to decrypt.");

            byte[] blob;
            try
            {
                blob = Convert.FromBase64String(base64Blob.Trim());
            }
            catch (FormatException)
            {
                throw new FormatException("That doesn't look like a valid Base64 string.");
            }

            var minLength = SaltSize + NonceSize + TagSize;
            if (blob.Length < minLength)
            {
                throw new FormatException("That Base64 text is too short to be data this tool encrypted.");
            }

            var salt = new byte[SaltSize];
            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ciphertext = new byte[blob.Length - minLength];

            var offset = 0;
            Buffer.BlockCopy(blob, offset, salt, 0, SaltSize); offset += SaltSize;
            Buffer.BlockCopy(blob, offset, nonce, 0, NonceSize); offset += NonceSize;
            Buffer.BlockCopy(blob, offset, tag, 0, TagSize); offset += TagSize;
            Buffer.BlockCopy(blob, offset, ciphertext, 0, ciphertext.Length);

            var key = DeriveKey(password, salt);
            var plaintextBytes = new byte[ciphertext.Length];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                try
                {
                    aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
                }
                catch (CryptographicException)
                {
                    // AES-GCM's authentication tag check failed - this is the expected, normal
                    // outcome for a wrong password (or genuinely corrupted/tampered ciphertext),
                    // not a bug - rethrown with a message that doesn't require the user to know
                    // what "the authentication tag" means.
                    throw new CryptographicException("Wrong password, or the encrypted text is corrupted/incomplete.");
                }
            }

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        private static byte[] DeriveKey(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);
    }
}
