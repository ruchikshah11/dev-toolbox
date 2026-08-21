using System.Text.RegularExpressions;

namespace DevToolbox.Tools.XmlEscape
{
    public static class XmlEscapeService
    {
        private static readonly Regex NumericEntity = new("&#(x[0-9A-Fa-f]+|[0-9]+);", RegexOptions.Compiled);

        public static string Escape(string input)
        {
            input ??= string.Empty;
            // "&" must be replaced first, otherwise the "&" introduced by the other
            // replacements below would themselves get escaped.
            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        public static string Unescape(string input)
        {
            input ??= string.Empty;

            // The multi-character entities must be unescaped before "&amp;", otherwise
            // "&amp;lt;" would incorrectly collapse to "<" instead of "&lt;".
            var result = input
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'")
                .Replace("&amp;", "&");

            result = NumericEntity.Replace(result, match =>
            {
                var body = match.Groups[1].Value;
                var code = body.StartsWith("x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToInt32(body.Substring(1), 16)
                    : int.Parse(body);
                return char.ConvertFromUtf32(code);
            });

            return result;
        }

        /// <summary>Wraps the input in a &lt;![CDATA[ ... ]]&gt; section, so it can carry raw markup/text through an XML document unescaped.</summary>
        public static string WrapInCData(string input)
        {
            input ??= string.Empty;
            // "]]>" can't appear literally inside a CDATA section - it's the closing delimiter -
            // so any occurrence is split across two adjacent sections instead: the first ends
            // right after the extra "]", and a new one immediately reopens with the rest.
            var escaped = input.Replace("]]>", "]]]]><![CDATA[>");
            return $"<![CDATA[{escaped}]]>";
        }

        /// <summary>Extracts the raw text out of a &lt;![CDATA[ ... ]]&gt; section, throwing if the input isn't wrapped that way.</summary>
        public static string ExtractFromCData(string input)
        {
            const string open = "<![CDATA[";
            const string close = "]]>";

            var trimmed = (input ?? string.Empty).Trim();
            if (!trimmed.StartsWith(open, StringComparison.Ordinal) || !trimmed.EndsWith(close, StringComparison.Ordinal))
            {
                throw new FormatException("Expected text wrapped in <![CDATA[ ... ]]>.");
            }

            return trimmed.Substring(open.Length, trimmed.Length - open.Length - close.Length);
        }
    }
}
