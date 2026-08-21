using System.Text.RegularExpressions;

namespace DevToolbox.UI
{
    public enum MarkupTokenKind { Text, TagBracket, TagName, AttributeName, AttributeEquals, AttributeValue, Comment, Doctype }

    public readonly record struct MarkupSegment(string Text, MarkupTokenKind Kind);

    /// <summary>
    /// A lightweight, regex-based tag-markup syntax highlighter - not a real parser. It only needs
    /// to classify substrings of exactly the original text (concatenating every segment's Text
    /// reproduces the input byte-for-byte) well enough to colorize tags/attributes/comments as you
    /// type or immediately after a format/transform runs. HTML and XML share the same
    /// tag/attribute grammar, so every markup-shaped pane in the app (HTML Viewer's editor, XML/
    /// HTML Formatter, XML Validator, XPath Tester, ...) tokenizes through this one class instead
    /// of each carrying its own copy. It doesn't handle every edge case a real parser would (e.g.
    /// a ">" inside a quoted attribute value can confuse the tag boundary, same limitation most
    /// simple highlighters have).
    /// </summary>
    public static class MarkupSyntaxTokenizer
    {
        private static readonly Regex TopLevel = new(
            @"(?<comment><!--[\s\S]*?-->)|(?<doctype><!DOCTYPE[^>]*>)|(?<tag></?[a-zA-Z][\w:\-]*(?:\s[^<>]*?)?\s*/?>)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TagInner = new(
            @"^(?<open></?)(?<name>[a-zA-Z][\w:\-]*)(?<attrs>.*?)(?<close>/?>)$",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex Attribute = new(
            @"(?<name>[a-zA-Z_:][\w:\-.]*)(?:(?<eq>\s*=\s*)(?<value>""[^""]*""|'[^']*'|[^\s""'=<>`]+))?",
            RegexOptions.Compiled);

        public static List<MarkupSegment> Tokenize(string markup)
        {
            markup ??= string.Empty;
            var segments = new List<MarkupSegment>();
            var lastEnd = 0;

            foreach (Match match in TopLevel.Matches(markup))
            {
                if (match.Index > lastEnd)
                {
                    segments.Add(new MarkupSegment(markup.Substring(lastEnd, match.Index - lastEnd), MarkupTokenKind.Text));
                }

                if (match.Groups["comment"].Success)
                {
                    segments.Add(new MarkupSegment(match.Value, MarkupTokenKind.Comment));
                }
                else if (match.Groups["doctype"].Success)
                {
                    segments.Add(new MarkupSegment(match.Value, MarkupTokenKind.Doctype));
                }
                else
                {
                    TokenizeTag(segments, match.Value);
                }

                lastEnd = match.Index + match.Length;
            }

            if (lastEnd < markup.Length)
            {
                segments.Add(new MarkupSegment(markup.Substring(lastEnd), MarkupTokenKind.Text));
            }

            return segments;
        }

        private static void TokenizeTag(List<MarkupSegment> segments, string tagText)
        {
            var inner = TagInner.Match(tagText);
            if (!inner.Success)
            {
                segments.Add(new MarkupSegment(tagText, MarkupTokenKind.Text));
                return;
            }

            segments.Add(new MarkupSegment(inner.Groups["open"].Value, MarkupTokenKind.TagBracket));
            segments.Add(new MarkupSegment(inner.Groups["name"].Value, MarkupTokenKind.TagName));

            var attrsText = inner.Groups["attrs"].Value;
            var pos = 0;
            foreach (Match attr in Attribute.Matches(attrsText))
            {
                if (attr.Index > pos)
                {
                    segments.Add(new MarkupSegment(attrsText.Substring(pos, attr.Index - pos), MarkupTokenKind.Text));
                }

                segments.Add(new MarkupSegment(attr.Groups["name"].Value, MarkupTokenKind.AttributeName));
                if (attr.Groups["eq"].Success)
                {
                    segments.Add(new MarkupSegment(attr.Groups["eq"].Value, MarkupTokenKind.AttributeEquals));
                    segments.Add(new MarkupSegment(attr.Groups["value"].Value, MarkupTokenKind.AttributeValue));
                }

                pos = attr.Index + attr.Length;
            }
            if (pos < attrsText.Length)
            {
                segments.Add(new MarkupSegment(attrsText.Substring(pos), MarkupTokenKind.Text));
            }

            segments.Add(new MarkupSegment(inner.Groups["close"].Value, MarkupTokenKind.TagBracket));
        }
    }
}
