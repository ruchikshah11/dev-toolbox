namespace DevToolbox.Tools.JsonFormatter
{
    internal enum JsoncTokenKind
    {
        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
        Colon,
        Comma,
        String,
        Literal,
        LineComment,
        BlockComment
    }

    // LinesBefore is how many newlines separated this token from the previous one in the
    // SOURCE text - JsoncPrinter uses it to tell a trailing "same-line" comment (LinesBefore
    // == 0) apart from a standalone comment that starts its own line (LinesBefore >= 1).
    internal readonly record struct JsoncToken(JsoncTokenKind Kind, string Text, int LinesBefore);

    // Lexes raw JSON/JSONC text into a flat token stream, keeping comments as first-class
    // tokens instead of discarding them - this is what lets the formatter preserve comments
    // through a reformat, which Newtonsoft's JObject/JArray model can't do (JObject drops
    // comments entirely, and JArray only keeps them as awkward pseudo-elements).
    internal static class JsoncTokenizer
    {
        public static List<JsoncToken> Tokenize(string input)
        {
            var tokens = new List<JsoncToken>();
            var i = 0;
            var n = input.Length;
            var linesBefore = 0;

            while (i < n)
            {
                var c = input[i];

                if (c == '\n')
                {
                    linesBefore++;
                    i++;
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                switch (c)
                {
                    case '{': Add(JsoncTokenKind.OpenBrace, "{"); continue;
                    case '}': Add(JsoncTokenKind.CloseBrace, "}"); continue;
                    case '[': Add(JsoncTokenKind.OpenBracket, "["); continue;
                    case ']': Add(JsoncTokenKind.CloseBracket, "]"); continue;
                    case ':': Add(JsoncTokenKind.Colon, ":"); continue;
                    case ',': Add(JsoncTokenKind.Comma, ","); continue;
                }

                if (c == '"')
                {
                    var start = i;
                    i++;
                    while (i < n && input[i] != '"')
                    {
                        if (input[i] == '\\' && i + 1 < n) i++;
                        else if (input[i] == '\n') throw new FormatException("Invalid JSON: a string literal is missing its closing quote.");
                        i++;
                    }
                    if (i >= n) throw new FormatException("Invalid JSON: a string literal is missing its closing quote.");
                    i++;
                    tokens.Add(new JsoncToken(JsoncTokenKind.String, input.Substring(start, i - start), linesBefore));
                    linesBefore = 0;
                    continue;
                }

                if (c == '/' && i + 1 < n && input[i + 1] == '/')
                {
                    var start = i;
                    i += 2;
                    while (i < n && input[i] != '\n') i++;
                    tokens.Add(new JsoncToken(JsoncTokenKind.LineComment, input.Substring(start, i - start).TrimEnd('\r'), linesBefore));
                    linesBefore = 0;
                    continue;
                }

                if (c == '/' && i + 1 < n && input[i + 1] == '*')
                {
                    var start = i;
                    i += 2;
                    while (i + 1 < n && !(input[i] == '*' && input[i + 1] == '/')) i++;
                    if (i + 1 >= n) throw new FormatException("Invalid JSON: a /* comment is missing its closing */.");
                    i += 2;
                    tokens.Add(new JsoncToken(JsoncTokenKind.BlockComment, input.Substring(start, i - start), linesBefore));
                    linesBefore = 0;
                    continue;
                }

                // A bare literal: a number, true, false, or null - run until the next
                // delimiter, structural char, or comment start.
                {
                    var start = i;
                    while (i < n
                           && !char.IsWhiteSpace(input[i])
                           && "{}[]:,\"".IndexOf(input[i]) < 0
                           && !(input[i] == '/' && i + 1 < n && (input[i + 1] == '/' || input[i + 1] == '*')))
                    {
                        i++;
                    }
                    tokens.Add(new JsoncToken(JsoncTokenKind.Literal, input.Substring(start, i - start), linesBefore));
                    linesBefore = 0;
                }

                void Add(JsoncTokenKind kind, string text)
                {
                    tokens.Add(new JsoncToken(kind, text, linesBefore));
                    linesBefore = 0;
                    i++;
                }
            }

            return tokens;
        }
    }
}
