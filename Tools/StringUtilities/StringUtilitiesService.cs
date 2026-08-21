using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DevToolbox.Tools.StringUtilities
{
    public static class StringUtilitiesService
    {
        /// <summary>Uppercases every character, invariant of culture.</summary>
        public static string ToUpper(string input) => (input ?? string.Empty).ToUpperInvariant();

        /// <summary>Lowercases every character, invariant of culture.</summary>
        public static string ToLower(string input) => (input ?? string.Empty).ToLowerInvariant();

        /// <summary>Title-cases each word (first letter up, rest down), invariant of culture.</summary>
        public static string ToTitleCase(string input) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase((input ?? string.Empty).ToLowerInvariant());

        /// <summary>Converts free text or any identifier casing into camelCase.</summary>
        public static string ToCamelCase(string input)
        {
            var words = SplitWords(input);
            if (words.Count == 0) return string.Empty;

            var sb = new StringBuilder(words[0]);
            for (var i = 1; i < words.Count; i++) sb.Append(Capitalize(words[i]));
            return sb.ToString();
        }

        /// <summary>Converts free text or any identifier casing into PascalCase.</summary>
        public static string ToPascalCase(string input) => string.Concat(SplitWords(input).Select(Capitalize));

        /// <summary>Converts free text or any identifier casing into snake_case.</summary>
        public static string ToSnakeCase(string input) => string.Join("_", SplitWords(input));

        /// <summary>Converts free text or any identifier casing into kebab-case.</summary>
        public static string ToKebabCase(string input) => string.Join("-", SplitWords(input));

        /// <summary>
        /// Converts free text into a URL-safe slug: transliterates accented characters to their
        /// plain-ASCII base (e.g. "é" -> "e"), then collapses every run of non-alphanumeric
        /// characters into a single hyphen. Unlike ToKebabCase, this strips punctuation outright
        /// rather than treating it as a word boundary to preserve, which is what a URL/filename
        /// actually needs.
        /// </summary>
        public static string ToSlug(string input)
        {
            input = (input ?? string.Empty).Trim();
            if (input.Length == 0) return string.Empty;

            var decomposed = input.Normalize(NormalizationForm.FormD);
            var withoutDiacritics = new string(decomposed
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            var slug = Regex.Replace(withoutDiacritics.ToLowerInvariant(), "[^a-z0-9]+", "-");
            return slug.Trim('-');
        }

        /// <summary>
        /// Breaks input into lowercase words regardless of its original casing style: splits on
        /// spaces/underscores/hyphens/dots, and also on camelCase/PascalCase/acronym boundaries
        /// (e.g. "HTTPServerName" -> "http", "server", "name") so any of the four case
        /// converters above can rebuild it in their own style.
        /// </summary>
        private static List<string> SplitWords(string input)
        {
            input = (input ?? string.Empty).Trim();
            if (input.Length == 0) return new List<string>();

            var normalized = Regex.Replace(input, @"[_\-.\s]+", " ");
            normalized = Regex.Replace(normalized, "(?<=[a-z0-9])(?=[A-Z])", " ");
            normalized = Regex.Replace(normalized, "(?<=[A-Z])(?=[A-Z][a-z])", " ");

            return normalized
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.ToLowerInvariant())
                .ToList();
        }

        /// <summary>Upper-cases just the first character of a lowercase word.</summary>
        private static string Capitalize(string word) =>
            word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word.Substring(1);

        /// <summary>Trims each line and collapses runs of spaces/tabs within it down to one space.</summary>
        public static string TrimAndCollapseWhitespace(string input)
        {
            input ??= string.Empty;
            var lines = input.Replace("\r\n", "\n").Split('\n')
                .Select(line => Regex.Replace(line.Trim(), @"[ \t]+", " "));
            return string.Join("\n", lines).Trim();
        }

        /// <summary>Reverses the character order of the whole input.</summary>
        public static string Reverse(string input)
        {
            input ??= string.Empty;
            // Simple char-array reversal - does not correctly handle surrogate pairs (e.g.
            // emoji outside the BMP) or combining character sequences, which would need
            // grapheme-cluster-aware reversal. Good enough for a v1 string utility.
            return new string(input.Reverse().ToArray());
        }

        /// <summary>Drops every line that's empty or whitespace-only.</summary>
        public static string RemoveBlankLines(string input)
        {
            input ??= string.Empty;
            var lines = input.Replace("\r\n", "\n").Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line));
            return string.Join("\n", lines);
        }

        // Standard "words per page" figure used by most online word-count tools
        // (250 words ~= one double-spaced page at 12pt).
        private const double WordsPerPage = 250d;

        // Average adult reading/speaking speeds (words per minute) used by most word-count
        // tools to estimate reading/speaking time.
        private const double ReadingWordsPerMinute = 200d;
        private const double SpeakingWordsPerMinute = 130d;

        /// <summary>Builds a multi-line character/word/sentence/paragraph/reading-time summary of the input.</summary>
        public static string Stats(string input)
        {
            input ??= string.Empty;
            var charCount = input.Length;
            var nonBlankCharCount = input.Count(c => !char.IsWhiteSpace(c));
            var words = input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = words.Length;
            var uniqueWordCount = words.Select(w => w.ToLowerInvariant()).Distinct().Count();
            var spaceCount = input.Count(c => c == ' ');
            var sentenceCount = Regex.Matches(input, @"[.!?]+").Count;
            var lines = input.Length == 0 ? Array.Empty<string>() : input.Replace("\r\n", "\n").Split('\n');
            var notEmptyLineCount = lines.Count(line => !string.IsNullOrWhiteSpace(line));
            var paragraphCount = input.Trim().Length == 0
                ? 0
                : Regex.Split(input.Replace("\r\n", "\n").Trim(), @"\n\s*\n").Count(p => !string.IsNullOrWhiteSpace(p));
            var pageCount = Math.Round(wordCount / WordsPerPage, 1);

            // Join with "\r\n", not "\n" - a native WinForms multiline TextBox only breaks
            // lines on a full CRLF, so a bare "\n" here renders as nothing and the lines
            // run together on screen.
            return string.Join("\r\n", new[]
            {
                $"Characters: {charCount}",
                $"Non Blank Characters: {nonBlankCharCount}",
                $"Words: {wordCount}",
                $"Unique Words: {uniqueWordCount}",
                $"Spaces: {spaceCount}",
                $"Sentences: {sentenceCount}",
                $"Paragraphs: {paragraphCount}",
                $"Lines: {lines.Length}",
                $"Not Empty Lines: {notEmptyLineCount}",
                $"Pages: {pageCount:0.0}",
                $"Reading Time: {FormatDuration(wordCount, ReadingWordsPerMinute)}",
                $"Speaking Time: {FormatDuration(wordCount, SpeakingWordsPerMinute)}"
            });
        }

        /// <summary>Formats a word count at the given reading/speaking speed as "X min Y sec".</summary>
        private static string FormatDuration(int wordCount, double wordsPerMinute)
        {
            if (wordCount == 0) return "0 sec";

            var totalSeconds = (int)Math.Ceiling(wordCount / wordsPerMinute * 60);
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;

            return minutes > 0 ? $"{minutes} min {seconds} sec" : $"{seconds} sec";
        }
    }
}
