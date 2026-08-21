using System.Security.Cryptography;

namespace DevToolbox.Tools.PasswordGenerator
{
    public enum PasswordStrength { Weak, Fair, Strong }

    /// <summary>
    /// Pure generation/scoring logic for the Password Generator tool: no WinForms references so
    /// it can be unit tested / reused on its own, matching the Service/Control split used by the
    /// other tools in this project. Uses a CSPRNG (RandomNumberGenerator), not System.Random -
    /// this tool's whole purpose is producing secrets, so predictable randomness would defeat it.
    /// </summary>
    public static class PasswordGeneratorService
    {
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string DigitChars = "0123456789";
        private const string SymbolChars = "!@#$%^&*()-_=+[]{};:,.<>?";

        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        // 128 unique words so each pick carries exactly log2(128) = 7 bits of entropy - a plain
        // English word bank, not a security-audited Diceware list, so passphrase entropy here is
        // a reasonable estimate for dev/test use rather than a guarantee for high-value secrets.
        private static readonly string[] WordBank =
        {
            "acorn", "amber", "anchor", "angle", "arbor", "arrow", "ashen", "aspen", "autumn", "azure",
            "badge", "baker", "barley", "basil", "beacon", "beetle", "birch", "bishop", "blaze", "blossom",
            "bluff", "boulder", "bramble", "brass", "breeze", "bridge", "bronze", "brook", "cabin", "candle",
            "canyon", "carbon", "cargo", "cascade", "cedar", "chalk", "channel", "charm", "cherry", "chestnut",
            "cider", "cinder", "clay", "cliff", "clover", "coast", "cobalt", "comet", "copper", "coral",
            "cotton", "cove", "crag", "crest", "crimson", "crocus", "crystal", "current", "dahlia", "dawn",
            "delta", "dewdrop", "dolphin", "dove", "drift", "dune", "dusk", "eagle", "ember", "emerald",
            "falcon", "fern", "ferry", "fjord", "flare", "flint", "forest", "fossil", "garnet", "glacier",
            "grove", "gravel", "harbor", "harvest", "hazel", "heather", "hickory", "hollow", "honey", "hyacinth",
            "ingot", "island", "ivory", "jasper", "juniper", "kestrel", "kettle", "knoll", "lagoon", "lantern",
            "ledge", "lilac", "linen", "lumber", "lyric", "maple", "marble", "marsh", "meadow", "mimosa",
            "mist", "moss", "nectar", "nettle", "nimbus", "oasis", "opal", "orchard", "orchid", "pebble",
            "petal", "pillar", "pine", "plateau", "prairie", "quartz", "quill", "ridge"
        };

        /// <summary>Generates a random password of the given length from the selected character types, guaranteeing at least one character of each selected type.</summary>
        public static string GeneratePassword(int length, bool lowercase, bool uppercase, bool digits, bool symbols)
        {
            if (length < 4 || length > 128)
            {
                throw new FormatException("Password length must be between 4 and 128 characters.");
            }

            var required = new List<char>();
            if (lowercase) required.Add(RandomChar(LowercaseChars));
            if (uppercase) required.Add(RandomChar(UppercaseChars));
            if (digits) required.Add(RandomChar(DigitChars));
            if (symbols) required.Add(RandomChar(SymbolChars));

            if (required.Count == 0)
            {
                throw new FormatException("Select at least one character type.");
            }
            if (required.Count > length)
            {
                throw new FormatException($"Length must be at least {required.Count} to include one of every selected character type.");
            }

            var pool = string.Concat(
                lowercase ? LowercaseChars : "",
                uppercase ? UppercaseChars : "",
                digits ? DigitChars : "",
                symbols ? SymbolChars : "");

            var chars = new char[length];
            for (var i = 0; i < required.Count; i++) chars[i] = required[i];
            for (var i = required.Count; i < length; i++) chars[i] = RandomChar(pool);

            Shuffle(chars);
            return new string(chars);
        }

        /// <summary>Generates a passphrase of random words joined by the given separator, optionally capitalizing each word and appending a random digit to one of them.</summary>
        public static string GeneratePassphrase(int wordCount, bool capitalize, bool includeNumber, string separator)
        {
            if (wordCount < 2 || wordCount > 12)
            {
                throw new FormatException("Word count must be between 2 and 12.");
            }

            var words = new string[wordCount];
            for (var i = 0; i < wordCount; i++)
            {
                var word = WordBank[RandomInt(WordBank.Length)];
                words[i] = capitalize ? char.ToUpperInvariant(word[0]) + word.Substring(1) : word;
            }

            if (includeNumber)
            {
                var index = RandomInt(words.Length);
                words[index] += RandomInt(10).ToString();
            }

            return string.Join(separator, words);
        }

        /// <summary>Estimates the entropy (in bits) of a password generated with the given length and character-type options.</summary>
        public static double PasswordEntropyBits(int length, bool lowercase, bool uppercase, bool digits, bool symbols)
        {
            var poolSize = 0;
            if (lowercase) poolSize += LowercaseChars.Length;
            if (uppercase) poolSize += UppercaseChars.Length;
            if (digits) poolSize += DigitChars.Length;
            if (symbols) poolSize += SymbolChars.Length;

            return poolSize <= 1 ? 0 : length * Math.Log(poolSize, 2);
        }

        /// <summary>Estimates the entropy (in bits) of a passphrase generated with the given word count, including the extra bits contributed by an appended digit.</summary>
        public static double PassphraseEntropyBits(int wordCount, bool includeNumber)
        {
            var bits = wordCount * Math.Log(WordBank.Length, 2);
            if (includeNumber) bits += Math.Log(wordCount, 2) + Math.Log(10, 2);
            return bits;
        }

        /// <summary>Buckets an entropy value into a Weak/Fair/Strong rating for display in the strength meter.</summary>
        public static PasswordStrength Classify(double entropyBits)
        {
            if (entropyBits >= 70) return PasswordStrength.Strong;
            if (entropyBits >= 45) return PasswordStrength.Fair;
            return PasswordStrength.Weak;
        }

        /// <summary>Picks one random character from the given pool string.</summary>
        private static char RandomChar(string pool) => pool[RandomInt(pool.Length)];

        /// <summary>Randomly reorders the given characters in place (Fisher-Yates), so the "guaranteed" characters aren't always in the same leading positions.</summary>
        private static void Shuffle(char[] chars)
        {
            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomInt(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
        }

        // Rejection sampling against a CSPRNG so every result in [0, exclusiveMax) is equally
        // likely - a plain "% exclusiveMax" over raw random bytes would skew low values slightly
        // more probable whenever exclusiveMax doesn't evenly divide 2^32.
        private static int RandomInt(int exclusiveMax)
        {
            var range = (uint)exclusiveMax;
            var threshold = uint.MaxValue - uint.MaxValue % range;
            var bytes = new byte[4];
            uint value;
            do
            {
                Rng.GetBytes(bytes);
                value = BitConverter.ToUInt32(bytes, 0);
            } while (value >= threshold);

            return (int)(value % range);
        }
    }
}
