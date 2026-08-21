using System.Text;

namespace DevToolbox.Tools.Base32Encoder
{
    public static class Base32EncoderService
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>Base32-encodes a UTF-8 string per RFC 4648, with '=' padding to a multiple of 8 characters.</summary>
        public static string Encode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            if (bytes.Length == 0) return string.Empty;

            var sb = new StringBuilder((bytes.Length + 4) / 5 * 8);
            var buffer = 0;
            var bitsLeft = 0;

            foreach (var b in bytes)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    sb.Append(Alphabet[(buffer >> bitsLeft) & 0x1F]);
                }
            }
            if (bitsLeft > 0)
            {
                sb.Append(Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
            }
            while (sb.Length % 8 != 0) sb.Append('=');

            return sb.ToString();
        }

        /// <summary>Decodes an RFC 4648 Base32 string back to its original UTF-8 text.</summary>
        public static string Decode(string input)
        {
            var trimmed = (input ?? string.Empty).Trim().TrimEnd('=').ToUpperInvariant();
            if (trimmed.Length == 0) return string.Empty;

            var bytes = new List<byte>();
            var buffer = 0;
            var bitsLeft = 0;

            foreach (var c in trimmed)
            {
                var index = Alphabet.IndexOf(c);
                if (index < 0) throw new FormatException($"'{c}' is not a valid Base32 character.");

                buffer = (buffer << 5) | index;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    bytes.Add((byte)((buffer >> bitsLeft) & 0xFF));
                }
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}
