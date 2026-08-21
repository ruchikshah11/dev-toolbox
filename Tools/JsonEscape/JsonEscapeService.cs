using Newtonsoft.Json;

namespace DevToolbox.Tools.JsonEscape
{
    public static class JsonEscapeService
    {
        // Deliberate exception to the "content only" convention used by the other escape
        // tools: a bare escaped string isn't valid/useful JSON without its surrounding
        // quotes, so Escape returns a full JSON string TOKEN, quotes included.
        public static string Escape(string input) => JsonConvert.ToString(input ?? string.Empty);

        // The input is expected to be a quoted JSON string token, e.g. "hello\nworld". For
        // convenience, if it isn't already wrapped in quotes it gets wrapped before parsing.
        public static string Unescape(string input)
        {
            input ??= string.Empty;
            var wrapped = input;
            if (!(wrapped.Length >= 2 && wrapped.StartsWith("\"") && wrapped.EndsWith("\"")))
            {
                wrapped = "\"" + wrapped + "\"";
            }

            try
            {
                return JsonConvert.DeserializeObject<string>(wrapped) ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new FormatException($"Could not parse input as a JSON string: {ex.Message}", ex);
            }
        }
    }
}
