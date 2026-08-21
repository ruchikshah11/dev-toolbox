using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DevToolbox.Tools.XmlFormatter
{
    public enum XmlIndentStyle
    {
        TwoSpaces,
        ThreeSpaces,
        FourSpaces,
        Tab,
        Compact
    }

    /// <summary>
    /// Pure text-in/text-out XML pretty-printing logic, kept separate from the UI (mirrors
    /// JsonFormatterService). XDocument.Parse (without PreserveWhitespace) drops insignificant
    /// whitespace between elements by default, which hands XmlWriter full control over
    /// re-indentation instead of fighting the original formatting.
    /// </summary>
    public static class XmlFormatterService
    {
        public static string Format(string xml, XmlIndentStyle indentStyle)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new FormatException("Nothing to format - paste some XML first.");
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                throw new FormatException($"Invalid XML: {ex.Message}", ex);
            }

            var compact = indentStyle == XmlIndentStyle.Compact;
            var sb = new StringBuilder();

            // XmlWriter over a StringWriter always reports its encoding as UTF-16 (the string
            // writer's Encoding), which would make the declaration lie about the actual bytes.
            // Writing the declaration manually keeps the output honest and consistent.
            if (doc.Declaration != null)
            {
                sb.Append("<?xml version=\"").Append(doc.Declaration.Version ?? "1.0").Append("\" encoding=\"UTF-8\"");
                if (!string.IsNullOrEmpty(doc.Declaration.Standalone))
                {
                    sb.Append(" standalone=\"").Append(doc.Declaration.Standalone).Append('"');
                }
                sb.Append("?>");
                if (!compact) sb.Append('\n');
            }

            var settings = new XmlWriterSettings
            {
                Indent = !compact,
                IndentChars = GetIndentUnit(indentStyle),
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Document
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }

            return sb.ToString();
        }

        private static string GetIndentUnit(XmlIndentStyle style) => style switch
        {
            XmlIndentStyle.TwoSpaces => "  ",
            XmlIndentStyle.ThreeSpaces => "   ",
            XmlIndentStyle.FourSpaces => "    ",
            XmlIndentStyle.Tab => "\t",
            _ => "  "
        };
    }
}
