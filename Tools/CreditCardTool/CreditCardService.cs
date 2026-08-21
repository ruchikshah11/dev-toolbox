using System.Text;

namespace DevToolbox.Tools.CreditCardTool
{
    /// <summary>
    /// Pure Luhn checksum, brand detection and test-number generation logic, kept separate
    /// from the UI so it can be unit tested without touching WinForms. Generated numbers are
    /// syntactically valid (correct length/prefix/checksum) TEST numbers only - they are not
    /// drawn from any real issuer range and must never be treated as real card numbers.
    /// </summary>
    public static class CreditCardService
    {
        public static bool IsValidLuhn(string number)
        {
            var digits = ExtractDigits(number);
            if (digits.Count == 0) return false;
            return ComputeLuhnSum(digits) % 10 == 0;
        }

        public static string DetectBrand(string number)
        {
            var digits = string.Concat(ExtractDigits(number));
            if (digits.Length == 0) return "Unknown";

            if (digits.StartsWith("4"))
            {
                return "Visa";
            }

            if (digits.Length >= 4 && int.TryParse(digits.Substring(0, 4), out var firstFour)
                && firstFour is >= 2221 and <= 2720)
            {
                return "Mastercard";
            }

            if (digits.Length >= 2 && int.TryParse(digits.Substring(0, 2), out var firstTwo))
            {
                if (firstTwo is >= 51 and <= 55) return "Mastercard";
                var firstTwoDigits = digits.Substring(0, 2);
                if (firstTwoDigits == "34" || firstTwoDigits == "37") return "American Express";
                if (firstTwoDigits == "65") return "Discover";
            }

            if (digits.Length >= 4 && digits.Substring(0, 4) == "6011")
            {
                return "Discover";
            }

            return "Unknown";
        }

        // Generates a syntactically-valid (correct length/prefix, Luhn-passing) fake test
        // number for the given brand. Not drawn from a real issuer's actual number space.
        public static string GenerateTestNumber(string brand, Random rng)
        {
            string prefix;
            int length;
            switch (brand)
            {
                case "Visa":
                    prefix = "4";
                    length = 16;
                    break;
                case "Mastercard":
                    prefix = (50 + rng.Next(1, 6)).ToString(); // 51-55
                    length = 16;
                    break;
                case "American Express":
                    prefix = rng.Next(0, 2) == 0 ? "34" : "37";
                    length = 15;
                    break;
                case "Discover":
                    prefix = "6011";
                    length = 16;
                    break;
                default:
                    throw new ArgumentException($"Unknown brand: {brand}");
            }

            var digits = new StringBuilder(prefix);
            var remaining = length - prefix.Length - 1; // leave the last digit for the check digit
            for (var i = 0; i < remaining; i++)
            {
                digits.Append(rng.Next(0, 10));
            }

            digits.Append(ComputeCheckDigit(digits.ToString()));
            return digits.ToString();
        }

        private static int ComputeCheckDigit(string partialNumber)
        {
            var digits = ExtractDigits(partialNumber);
            digits.Add(0); // placeholder for the not-yet-known check digit
            var mod = ComputeLuhnSum(digits) % 10;
            return mod == 0 ? 0 : 10 - mod;
        }

        // Standard Luhn sum: starting from the rightmost digit, every second digit is doubled
        // (and reduced by 9 if the doubled value exceeds 9); the number is valid when the sum
        // is a multiple of 10.
        private static int ComputeLuhnSum(List<int> digits)
        {
            var sum = 0;
            var alternate = false;
            for (var i = digits.Count - 1; i >= 0; i--)
            {
                var d = digits[i];
                if (alternate)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                sum += d;
                alternate = !alternate;
            }
            return sum;
        }

        private static List<int> ExtractDigits(string number)
        {
            var digits = new List<int>();
            if (string.IsNullOrEmpty(number)) return digits;
            foreach (var c in number)
            {
                if (char.IsDigit(c)) digits.Add(c - '0');
            }
            return digits;
        }
    }
}
