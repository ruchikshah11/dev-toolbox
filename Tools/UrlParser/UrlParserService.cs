using System.Net;
using System.Text;

namespace DevToolbox.Tools.UrlParser
{
    public static class UrlParserService
    {
        public static string Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new FormatException("Enter a URL to parse.");
            }

            Uri uri;
            try
            {
                uri = new Uri(input.Trim());
            }
            catch (UriFormatException ex)
            {
                throw new FormatException($"Invalid URL: {ex.Message}");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Scheme: {uri.Scheme}");
            sb.AppendLine($"Host: {uri.Host}");
            sb.AppendLine($"Port: {uri.Port}");
            sb.AppendLine($"Path: {(string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath)}");
            sb.AppendLine($"Fragment: {(string.IsNullOrEmpty(uri.Fragment) ? "(none)" : uri.Fragment.TrimStart('#'))}");
            sb.AppendLine();
            sb.AppendLine("Query Parameters:");

            var query = uri.Query;
            if (string.IsNullOrEmpty(query) || query == "?")
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                query = query.TrimStart('?');
                var pairs = query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var idx = pair.IndexOf('=');
                    string key, value;
                    if (idx >= 0)
                    {
                        key = pair.Substring(0, idx);
                        value = pair.Substring(idx + 1);
                    }
                    else
                    {
                        key = pair;
                        value = string.Empty;
                    }

                    key = WebUtility.UrlDecode(key) ?? key;
                    value = WebUtility.UrlDecode(value) ?? value;
                    sb.AppendLine($"  {key} = {value}");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
