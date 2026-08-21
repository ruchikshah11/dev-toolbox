using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DevToolbox.Tools.XPathTester
{
    /// <summary>
    /// Pure XML/XPath evaluation logic, kept separate from the UI so it can be unit tested
    /// without touching WinForms.
    /// </summary>
    public static class XPathTesterService
    {
        public static string Evaluate(string xml, string xpath)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new FormatException("Paste an XML document first.");
            }

            if (string.IsNullOrWhiteSpace(xpath))
            {
                throw new FormatException("Enter an XPath expression first.");
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                throw new FormatException(
                    $"Invalid XML at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
            }

            var navigator = doc.CreateNavigator();
            object result;
            try
            {
                result = navigator.Evaluate(xpath);
            }
            catch (XPathException ex)
            {
                throw new FormatException($"Invalid XPath expression: {ex.Message}", ex);
            }

            var lines = new List<string>();
            if (result is XPathNodeIterator iterator)
            {
                var matches = new List<string>();
                while (iterator.MoveNext())
                {
                    matches.Add(iterator.Current!.Value);
                }

                lines.Add($"{matches.Count} match(es) found:");
                lines.Add(string.Empty);
                for (var i = 0; i < matches.Count; i++)
                {
                    lines.Add($"[{i + 1}] {matches[i]}");
                }
            }
            else
            {
                // Scalar XPath results (boolean(), count(), string(), number() expressions, ...).
                lines.Add("Result:");
                lines.Add(Convert.ToString(result) ?? string.Empty);
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
