using System.Net;
using System.Text;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace DevToolbox.Tools.HtmlFormatter
{
    public enum HtmlIndentStyle
    {
        TwoSpaces,
        ThreeSpaces,
        FourSpaces,
        Tab,
        Compact
    }

    /// <summary>
    /// Pure text-in/text-out HTML pretty-printing logic, kept separate from the UI. Parses with
    /// HtmlAgilityPack (very tolerant of real-world, not-quite-valid markup) and re-serializes
    /// with a hand-rolled recursive writer so indentation is fully under our control.
    /// This is a heuristic re-indenter, not a full HTML normalizer: it collapses insignificant
    /// whitespace and re-flows element structure, but does not try to preserve every original
    /// whitespace-significant nuance (e.g. inline element spacing).
    /// </summary>
    public static class HtmlFormatterService
    {
        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input",
            "link", "meta", "param", "source", "track", "wbr"
        };

        // Elements whose inner content must be passed through verbatim - re-indenting the
        // contents of a <script>/<style>/<pre>/<textarea> would change its meaning.
        private static readonly HashSet<string> RawTextElements = new(StringComparer.OrdinalIgnoreCase)
        {
            "pre", "textarea", "script", "style"
        };

        public static string Format(string html, HtmlIndentStyle indentStyle)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new FormatException("Nothing to format - paste some HTML first.");
            }

            var doc = new HtmlDocument { OptionOutputAsXml = false };
            try
            {
                doc.LoadHtml(html);
            }
            catch (Exception ex)
            {
                // HtmlAgilityPack is intentionally lenient and rarely throws even for broken
                // markup, but wrap anything it does throw in a friendly message rather than
                // letting a raw exception dialog surface.
                throw new FormatException($"Could not parse HTML: {ex.Message}", ex);
            }

            if (doc.DocumentNode is null || !doc.DocumentNode.HasChildNodes)
            {
                throw new FormatException("No HTML content was found to format.");
            }

            var compact = indentStyle == HtmlIndentStyle.Compact;
            var indentUnit = GetIndentUnit(indentStyle);

            var sb = new StringBuilder();
            foreach (var child in doc.DocumentNode.ChildNodes)
            {
                WriteNode(sb, child, 0, indentUnit, compact);
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }

        private static string GetIndentUnit(HtmlIndentStyle style) => style switch
        {
            HtmlIndentStyle.TwoSpaces => "  ",
            HtmlIndentStyle.ThreeSpaces => "   ",
            HtmlIndentStyle.FourSpaces => "    ",
            HtmlIndentStyle.Tab => "\t",
            _ => "  "
        };

        private static void WriteNode(StringBuilder sb, HtmlNode node, int depth, string indentUnit, bool compact)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    AppendIndent(sb, depth, indentUnit, compact);
                    sb.Append(node.OuterHtml.Trim());
                    AppendNewLine(sb, compact);
                    return;

                case HtmlNodeType.Text:
                    var text = node.InnerText;
                    if (string.IsNullOrWhiteSpace(text)) return; // drop insignificant whitespace-only text nodes
                    AppendIndent(sb, depth, indentUnit, compact);
                    sb.Append(WebUtility.HtmlEncode(text.Trim()));
                    AppendNewLine(sb, compact);
                    return;

                case HtmlNodeType.Element:
                    WriteElement(sb, node, depth, indentUnit, compact);
                    return;

                default:
                    return; // Document/other node types carry no direct output of their own.
            }
        }

        private static void WriteElement(StringBuilder sb, HtmlNode node, int depth, string indentUnit, bool compact)
        {
            AppendIndent(sb, depth, indentUnit, compact);
            sb.Append('<').Append(node.Name);
            foreach (var attr in node.Attributes)
            {
                sb.Append(' ').Append(attr.Name);
                if (attr.Value != null)
                {
                    sb.Append("=\"").Append(WebUtility.HtmlEncode(attr.Value)).Append('"');
                }
            }

            if (VoidElements.Contains(node.Name))
            {
                sb.Append(" />");
                AppendNewLine(sb, compact);
                return;
            }

            sb.Append('>');

            if (RawTextElements.Contains(node.Name))
            {
                sb.Append(node.InnerHtml);
                sb.Append("</").Append(node.Name).Append('>');
                AppendNewLine(sb, compact);
                return;
            }

            var meaningfulChildren = node.ChildNodes
                .Where(c => c.NodeType != HtmlNodeType.Text || !string.IsNullOrWhiteSpace(c.InnerText))
                .ToList();

            if (meaningfulChildren.Count == 0)
            {
                sb.Append("</").Append(node.Name).Append('>');
                AppendNewLine(sb, compact);
                return;
            }

            // Keep a single text child inline (e.g. <p>Hello</p>) instead of exploding it onto
            // its own indented line.
            if (meaningfulChildren.Count == 1 && meaningfulChildren[0].NodeType == HtmlNodeType.Text)
            {
                sb.Append(WebUtility.HtmlEncode(meaningfulChildren[0].InnerText.Trim()));
                sb.Append("</").Append(node.Name).Append('>');
                AppendNewLine(sb, compact);
                return;
            }

            AppendNewLine(sb, compact);
            foreach (var child in meaningfulChildren)
            {
                WriteNode(sb, child, depth + 1, indentUnit, compact);
            }
            AppendIndent(sb, depth, indentUnit, compact);
            sb.Append("</").Append(node.Name).Append('>');
            AppendNewLine(sb, compact);
        }

        private static void AppendIndent(StringBuilder sb, int depth, string unit, bool compact)
        {
            if (compact || depth <= 0) return;
            for (var i = 0; i < depth; i++) sb.Append(unit);
        }

        private static void AppendNewLine(StringBuilder sb, bool compact)
        {
            if (!compact) sb.Append('\n');
        }
    }
}
