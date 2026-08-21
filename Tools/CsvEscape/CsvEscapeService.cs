namespace DevToolbox.Tools.CsvEscape
{
    // Treats the whole input as a single RFC4180 CSV field value.
    public static class CsvEscapeService
    {
        private static readonly char[] SpecialChars = { ',', '"', '\n', '\r' };

        public static string Escape(string input)
        {
            input ??= string.Empty;
            if (input.IndexOfAny(SpecialChars) >= 0)
            {
                return "\"" + input.Replace("\"", "\"\"") + "\"";
            }
            return input;
        }

        public static string Unescape(string input)
        {
            input ??= string.Empty;
            if (input.Length >= 2 && input.StartsWith("\"") && input.EndsWith("\""))
            {
                var inner = input.Substring(1, input.Length - 2);
                return inner.Replace("\"\"", "\"");
            }
            return input;
        }
    }
}
