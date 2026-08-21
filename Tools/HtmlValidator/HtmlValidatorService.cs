using System.Text;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace DevToolbox.Tools.HtmlValidator
{
    /// <summary>
    /// Pure HTML structural validation, kept separate from the UI so it can be unit tested
    /// without touching WinForms. This relies on HtmlAgilityPack's lenient, forgiving parser
    /// (it silently repairs many real-world markup problems), so it reports structural parse
    /// errors it noticed rather than performing strict W3C validation - it will not catch every
    /// issue a real validator or browser would flag.
    /// </summary>
    public static class HtmlValidatorService
    {
        public static string Validate(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new FormatException("Nothing to validate - paste an HTML document first.");
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var errors = doc.ParseErrors?.ToList() ?? new List<HtmlParseError>();
            var sb = new StringBuilder();

            if (errors.Count == 0)
            {
                sb.AppendLine("No structural issues found.");
            }
            else
            {
                sb.AppendLine($"{errors.Count} issue(s) found:");
                sb.AppendLine();
                foreach (var err in errors)
                {
                    sb.AppendLine($"Line {err.Line}, position {err.LinePosition} [{err.Code}]: {err.Reason}");
                }
            }

            return sb.ToString();
        }
    }
}
