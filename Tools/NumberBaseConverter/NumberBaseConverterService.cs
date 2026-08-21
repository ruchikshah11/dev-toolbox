using System.Text;

namespace DevToolbox.Tools.NumberBaseConverter
{
    public readonly record struct NumberBaseResult(string Binary, string Octal, string Decimal, string Hexadecimal);

    public static class NumberBaseConverterService
    {
        public static readonly string[] Bases = { "Binary (base 2)", "Octal (base 8)", "Decimal (base 10)", "Hexadecimal (base 16)" };

        private const string DigitAlphabet = "0123456789ABCDEF";

        /// <summary>Parses a number in the given base and returns its binary/octal/decimal/hex forms.</summary>
        public static NumberBaseResult Convert(string input, string fromBase)
        {
            input = (input ?? string.Empty).Trim();
            if (input.Length == 0) throw new FormatException("Enter a number to convert.");

            var radix = RadixFor(fromBase);
            var stripped = StripPrefix(input, radix);

            long value;
            try
            {
                value = ParseInBase(stripped, radix);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new FormatException($"'{input}' is not a valid {fromBase} number (or it's too large to fit in 64 bits).", ex);
            }

            return new NumberBaseResult(
                ToBaseString(value, 2),
                ToBaseString(value, 8),
                value.ToString(),
                ToBaseString(value, 16));
        }

        /// <summary>Maps a "Binary (base 2)"-style label to its numeric radix.</summary>
        private static int RadixFor(string label) => label switch
        {
            "Binary (base 2)" => 2,
            "Octal (base 8)" => 8,
            "Decimal (base 10)" => 10,
            "Hexadecimal (base 16)" => 16,
            _ => throw new ArgumentException($"Unknown base '{label}'.", nameof(label))
        };

        // Accepts an optional 0x/0b/0o prefix regardless of the selected base, since pasting a
        // "0x1F"-style literal while Hexadecimal is selected is the most natural way a developer
        // would use this tool.
        private static string StripPrefix(string digits, int radix)
        {
            var negative = digits.StartsWith("-");
            var body = negative ? digits.Substring(1) : digits;
            var trimmedBody = radix switch
            {
                2 when body.StartsWith("0b", StringComparison.OrdinalIgnoreCase) => body.Substring(2),
                8 when body.StartsWith("0o", StringComparison.OrdinalIgnoreCase) => body.Substring(2),
                16 when body.StartsWith("0x", StringComparison.OrdinalIgnoreCase) => body.Substring(2),
                _ => body
            };
            return negative ? "-" + trimmedBody : trimmedBody;
        }

        /// <summary>Parses a (possibly negative) magnitude string in the given radix into a signed long.</summary>
        private static long ParseInBase(string digits, int radix)
        {
            var negative = digits.StartsWith("-");
            if (negative) digits = digits.Substring(1);
            if (digits.Length == 0) throw new FormatException("no digits");

            long value = 0;
            foreach (var c in digits)
            {
                var digitValue = DigitAlphabet.IndexOf(char.ToUpperInvariant(c));
                if (digitValue < 0 || digitValue >= radix) throw new FormatException($"invalid digit '{c}'");
                value = checked(value * radix + digitValue);
            }
            return negative ? -value : value;
        }

        /// <summary>Renders a signed long as a magnitude string (with a leading '-' if negative) in the given radix.</summary>
        private static string ToBaseString(long value, int radix)
        {
            if (value == 0) return "0";

            var negative = value < 0;
            var magnitude = negative ? (ulong)(-value) : (ulong)value;

            var sb = new StringBuilder();
            while (magnitude > 0)
            {
                sb.Insert(0, DigitAlphabet[(int)(magnitude % (ulong)radix)]);
                magnitude /= (ulong)radix;
            }
            return negative ? "-" + sb : sb.ToString();
        }
    }
}
