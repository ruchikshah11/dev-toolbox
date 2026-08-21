namespace DevToolbox.Tools.JsonFormatter
{
    // Colors raw, as-typed JSON/JSONC input the same way JsoncPrinter colors the *formatted*
    // output - but preserves the original text and whitespace verbatim (no reflow), since this
    // runs against text that may not even be valid JSON yet (mid-keystroke).
    internal static class JsonInputColorizer
    {
        public static List<JsonSegment> BuildSegments(string input)
        {
            var segments = new List<JsonSegment>();

            List<JsoncToken> tokens;
            try
            {
                tokens = JsoncTokenizer.Tokenize(input);
            }
            catch (FormatException)
            {
                // An unterminated string/comment mid-keystroke - leave the text uncolored rather
                // than let a single keystroke throw; the next keystroke usually resolves it.
                segments.Add(new JsonSegment(input, JsonTokenKind.Structural));
                return segments;
            }

            var pos = 0;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var start = input.IndexOf(token.Text, pos, StringComparison.Ordinal);
                if (start < 0) start = pos;

                if (start > pos)
                {
                    segments.Add(new JsonSegment(input.Substring(pos, start - pos), JsonTokenKind.Whitespace));
                }

                segments.Add(Classify(token, tokens, i));
                pos = start + token.Text.Length;
            }

            if (pos < input.Length)
            {
                segments.Add(new JsonSegment(input.Substring(pos), JsonTokenKind.Whitespace));
            }

            return segments;
        }

        private static JsonSegment Classify(JsoncToken token, List<JsoncToken> tokens, int i) => token.Kind switch
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
    }
}
