using System.Net;

namespace DevToolbox.Tools.UrlEncoder
{
    public static class UrlEncoderService
    {
        public static string Encode(string input) => WebUtility.UrlEncode(input ?? string.Empty) ?? string.Empty;

        public static string Decode(string input) => WebUtility.UrlDecode(input ?? string.Empty) ?? string.Empty;
    }
}
