using System.Security.Cryptography;
using System.Text;

namespace DevToolbox.Tools.MessageDigester
{
    public static class MessageDigesterService
    {
        public static string Md5(string input) => Hash(input, MD5.Create());

        public static string Sha256(string input) => Hash(input, SHA256.Create());

        public static string Sha512(string input) => Hash(input, SHA512.Create());

        private static string Hash(string input, HashAlgorithm algorithm)
        {
            using (algorithm)
            {
                var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
                return string.Concat(bytes.Select(b => b.ToString("x2")));
            }
        }
    }
}
