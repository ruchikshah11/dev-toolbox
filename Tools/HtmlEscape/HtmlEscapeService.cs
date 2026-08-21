using System.Net;

namespace DevToolbox.Tools.HtmlEscape
{
    public static class HtmlEscapeService
    {
        public static string Escape(string input) => WebUtility.HtmlEncode(input ?? string.Empty) ?? string.Empty;

        public static string Unescape(string input) => WebUtility.HtmlDecode(input ?? string.Empty) ?? string.Empty;
    }
}
