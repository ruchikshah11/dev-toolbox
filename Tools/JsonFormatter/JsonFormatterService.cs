using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevToolbox.Tools.JsonFormatter
{
    /// <summary>
    /// Pure text-in/segments-out formatting logic, kept separate from the UI so it can be unit
    /// tested or reused (e.g. by a future CLI front-end) without touching WinForms. Producing
    /// tagged segments (rather than a plain string) lets the UI colorize the exact same output
    /// it displays as text, instead of re-parsing rendered text with a second pass.
    /// </summary>
    public static class JsonFormatterService
    {
        public static JToken Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Nothing to format - paste some JSON or choose a file first.");
            }

            try
            {
                return JToken.Parse(json);
            }
            catch (JsonReaderException ex)
            {
                throw new FormatException($"Invalid JSON: {ex.Message}", ex);
            }
        }

        public static string Format(JToken token, JsonIndentStyle indentStyle, JsonBracketStyle bracketStyle) =>
            string.Concat(FormatSegments(token, indentStyle, bracketStyle).Select(s => s.Text));

        /// <summary>
        /// Compact/JS-escaped output only - both are inherently single-line and JSON-spec-strict,
        /// so comments are dropped here by design (that's what makes the output valid, minified
        /// JSON). For the indented styles, which DO preserve comments, use
        /// <see cref="FormatSegmentsPreservingComments"/> instead, which re-lexes the raw source
        /// text rather than walking this JToken (Newtonsoft's object model can't carry comments
        /// through re-serialization - JObject drops them entirely).
        /// </summary>
        public static List<JsonSegment> FormatSegments(JToken token, JsonIndentStyle indentStyle, JsonBracketStyle bracketStyle)
        {
            var segments = new List<JsonSegment>();

            switch (indentStyle)
            {
                case JsonIndentStyle.Compact:
                    WriteCompact(segments, token);
                    break;

                case JsonIndentStyle.JavaScriptEscaped:
                    var compactSegments = new List<JsonSegment>();
                    WriteCompact(compactSegments, token);
                    var compactText = string.Concat(compactSegments.Select(s => s.Text));
                    segments.Add(new JsonSegment(JsonConvert.ToString(compactText), JsonTokenKind.StringValue));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(indentStyle),
                        "Indented styles preserve comments and go through FormatSegmentsPreservingComments instead.");
            }

            return segments;
        }

        /// <summary>
        /// Re-indents raw JSON/JSONC source text (2/3/4 spaces or tab), carrying any "//" or
        /// "/* */" comments straight through at their original position - see JsoncTokenizer
        /// and JsoncPrinter for how. Only valid for the indented styles (not Compact or
        /// JavaScriptEscaped, which strip comments by design).
        /// </summary>
        public static List<JsonSegment> FormatSegmentsPreservingComments(string rawJson, JsonIndentStyle indentStyle, JsonBracketStyle bracketStyle)
        {
            var tokens = JsoncTokenizer.Tokenize(rawJson);
            return JsoncPrinter.Print(tokens, indentStyle, bracketStyle);
        }

        private static void WriteCompact(List<JsonSegment> segs, JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var obj = (JObject)token;
                    var props = obj.Properties().ToList();
                    segs.Add(new JsonSegment("{", JsonTokenKind.Structural));
                    for (var i = 0; i < props.Count; i++)
                    {
                        segs.Add(new JsonSegment(JsonConvert.ToString(props[i].Name), JsonTokenKind.Key));
                        segs.Add(new JsonSegment(":", JsonTokenKind.Structural));
                        WriteCompact(segs, props[i].Value);
                        if (i < props.Count - 1) segs.Add(new JsonSegment(",", JsonTokenKind.Structural));
                    }
                    segs.Add(new JsonSegment("}", JsonTokenKind.Structural));
                    break;

                case JTokenType.Array:
                    var arr = (JArray)token;
                    segs.Add(new JsonSegment("[", JsonTokenKind.Structural));
                    for (var i = 0; i < arr.Count; i++)
                    {
                        WriteCompact(segs, arr[i]);
                        if (i < arr.Count - 1) segs.Add(new JsonSegment(",", JsonTokenKind.Structural));
                    }
                    segs.Add(new JsonSegment("]", JsonTokenKind.Structural));
                    break;

                default:
                    AppendScalar(segs, (JValue)token);
                    break;
            }
        }

        private static void AppendScalar(List<JsonSegment> segs, JValue value)
        {
            var kind = value.Type switch
            {
                JTokenType.String => JsonTokenKind.StringValue,
                JTokenType.Integer or JTokenType.Float => JsonTokenKind.Number,
                JTokenType.Boolean => JsonTokenKind.Boolean,
                JTokenType.Null => JsonTokenKind.Null,
                _ => JsonTokenKind.StringValue
            };
            // JToken.ToString(Formatting.None) renders a valid JSON literal for any scalar
            // (quoted/escaped for strings), so there's no need to special-case each kind here.
            segs.Add(new JsonSegment(value.ToString(Formatting.None), kind));
        }
    }
}
