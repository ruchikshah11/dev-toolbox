using System.Text;

namespace DevToolbox.Tools.JsonFormatter
{
    // Re-indents a token stream produced by JsoncTokenizer, carrying comments straight through
    // unchanged. Unlike JsonFormatterService's JToken-based writer (which only knows about
    // real JSON values), this operates purely on tokens, so a comment is just another token
    // with its own line-break rule - no special-casing needed for "comment inside an object"
    // vs "comment inside an array".
    internal static class JsoncPrinter
    {
        public static List<JsonSegment> Print(List<JsoncToken> tokens, JsonIndentStyle indentStyle, JsonBracketStyle bracketStyle)
        {
            var indentUnit = GetIndentUnit(indentStyle);
            var segments = new List<JsonSegment>();
            var depth = 0;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var prevKind = i > 0 ? tokens[i - 1].Kind : (JsoncTokenKind?)null;

                if (token.Kind is JsoncTokenKind.CloseBrace or JsoncTokenKind.CloseBracket)
                {
                    var matchingOpen = token.Kind == JsoncTokenKind.CloseBrace ? JsoncTokenKind.OpenBrace : JsoncTokenKind.OpenBracket;
                    var isEmpty = prevKind == matchingOpen;
                    if (!isEmpty)
                    {
                        depth--;
                        segments.Add(new JsonSegment("\n" + Repeat(indentUnit, depth), JsonTokenKind.Whitespace));
                    }
                    segments.Add(new JsonSegment(token.Text, JsonTokenKind.Structural));
                    continue;
                }

                var lineBreakBefore = false;
                var spaceBefore = false;

                if (i == 0)
                {
                    // first token - nothing precedes it
                }
                else if (token.Kind is JsoncTokenKind.LineComment or JsoncTokenKind.BlockComment)
                {
                    lineBreakBefore = token.LinesBefore >= 1;
                    spaceBefore = !lineBreakBefore;
                }
                else if (token.Kind is JsoncTokenKind.Colon or JsoncTokenKind.Comma)
                {
                    lineBreakBefore = false;
                }
                else if (token.Kind is JsoncTokenKind.OpenBrace or JsoncTokenKind.OpenBracket)
                {
                    lineBreakBefore = prevKind == JsoncTokenKind.Colon
                        ? bracketStyle == JsonBracketStyle.Expanded
                        : true;
                    spaceBefore = !lineBreakBefore && prevKind == JsoncTokenKind.Colon;
                }
                else // String or Literal
                {
                    if (prevKind == JsoncTokenKind.Colon)
                    {
                        lineBreakBefore = false;
                        spaceBefore = true;
                    }
                    else
                    {
                        lineBreakBefore = true;
                    }
                }

                if (lineBreakBefore)
                {
                    segments.Add(new JsonSegment("\n" + Repeat(indentUnit, depth), JsonTokenKind.Whitespace));
                }
                else if (spaceBefore)
                {
                    segments.Add(new JsonSegment(" ", JsonTokenKind.Whitespace));
                }

                segments.Add(ToSegment(token, i, tokens));

                if (token.Kind is JsoncTokenKind.OpenBrace or JsoncTokenKind.OpenBracket)
                {
                    var matchingClose = token.Kind == JsoncTokenKind.OpenBrace ? JsoncTokenKind.CloseBrace : JsoncTokenKind.CloseBracket;
                    var isEmpty = i + 1 < tokens.Count && tokens[i + 1].Kind == matchingClose;
                    if (!isEmpty) depth++;
                }
            }

            return segments;
        }

        private static JsonSegment ToSegment(JsoncToken token, int i, List<JsoncToken> tokens) => token.Kind switch
        {
            JsoncTokenKind.String => new JsonSegment(token.Text,
                i + 1 < tokens.Count && tokens[i + 1].Kind == JsoncTokenKind.Colon ? JsonTokenKind.Key : JsonTokenKind.StringValue),
            JsoncTokenKind.Literal => new JsonSegment(token.Text, ClassifyLiteral(token.Text)),
            JsoncTokenKind.LineComment => new JsonSegment(token.Text, JsonTokenKind.Comment),
            JsoncTokenKind.BlockComment => new JsonSegment(token.Text, JsonTokenKind.Comment),
            _ => new JsonSegment(token.Text, JsonTokenKind.Structural)
        };

        private static JsonTokenKind ClassifyLiteral(string text) => text switch
        {
            "true" => JsonTokenKind.Boolean,
            "false" => JsonTokenKind.Boolean,
            "null" => JsonTokenKind.Null,
            _ => JsonTokenKind.Number
        };

        private static string GetIndentUnit(JsonIndentStyle style) => style switch
        {
            JsonIndentStyle.TwoSpaces => "  ",
            JsonIndentStyle.ThreeSpaces => "   ",
            JsonIndentStyle.FourSpaces => "    ",
            JsonIndentStyle.Tab => "\t",
            _ => "  "
        };

        private static string Repeat(string unit, int depth)
        {
            if (depth <= 0) return string.Empty;
            var sb = new StringBuilder(unit.Length * depth);
            for (var i = 0; i < depth; i++) sb.Append(unit);
            return sb.ToString();
        }
    }
}
