using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevToolbox.Tools.JwtDecoder
{
    public readonly record struct JwtDecodeResult(string HeaderJson, string PayloadJson, string ClaimsSummary);

    public static class JwtDecoderService
    {
        public static JwtDecodeResult Decode(string token)
        {
            var parts = SplitParts(token);

            var headerJson = PrettyPrint(DecodeSegment(parts[0], "header"));
            var payloadRaw = DecodeSegment(parts[1], "payload");
            var payloadJson = PrettyPrint(payloadRaw);
            var claimsSummary = BuildClaimsSummary((JObject)JToken.Parse(payloadRaw));

            return new JwtDecodeResult(headerJson, payloadJson, claimsSummary);
        }

        // Returns null (rather than throwing) when the algorithm isn't a shared-secret one
        // (e.g. RS256/ES256 need a public key, which this tool doesn't collect), so the caller
        // can show "not applicable" instead of a false failure.
        public static bool? VerifySignature(string token, string secret)
        {
            if (string.IsNullOrEmpty(secret)) return null;

            var parts = SplitParts(token);
            var header = (JObject)JToken.Parse(DecodeSegment(parts[0], "header"));
            var algorithm = header["alg"]?.ToString() ?? string.Empty;

            using HMAC? hmac = algorithm.ToUpperInvariant() switch
            {
                "HS256" => new HMACSHA256(Encoding.UTF8.GetBytes(secret)),
                "HS384" => new HMACSHA384(Encoding.UTF8.GetBytes(secret)),
                "HS512" => new HMACSHA512(Encoding.UTF8.GetBytes(secret)),
                _ => null
            };
            if (hmac is null) return null;

            var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
            var expected = Base64UrlEncode(hmac.ComputeHash(signingInput));
            return expected == parts[2];
        }

        private static string[] SplitParts(string token)
        {
            token = (token ?? string.Empty).Trim();
            if (token.Length == 0) throw new FormatException("Paste a JWT to decode.");

            var parts = token.Split('.');
            if (parts.Length != 3) throw new FormatException("A JWT must have three dot-separated parts (header.payload.signature).");
            return parts;
        }

        private static string DecodeSegment(string segment, string name)
        {
            try
            {
                return Encoding.UTF8.GetString(Base64UrlDecode(segment));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                throw new FormatException($"Could not base64url-decode the {name} segment.", ex);
            }
        }

        private static string PrettyPrint(string json)
        {
            try
            {
                return JToken.Parse(json).ToString(Formatting.Indented);
            }
            catch (JsonReaderException ex)
            {
                throw new FormatException($"Segment did not contain valid JSON: {ex.Message}", ex);
            }
        }

        private static string BuildClaimsSummary(JObject payload)
        {
            var lines = new List<string>();
            AppendTimeClaim(lines, payload, "iat", "Issued At");
            AppendTimeClaim(lines, payload, "nbf", "Not Before");
            AppendTimeClaim(lines, payload, "exp", "Expires");

            if (payload["exp"] is { } expToken && expToken.Type is JTokenType.Integer or JTokenType.Float)
            {
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds((long)expToken);
                lines.Add(expiresAt <= DateTimeOffset.UtcNow ? "Status: EXPIRED" : "Status: Valid (not yet expired)");
            }

            return lines.Count == 0 ? "(no iat/nbf/exp claims found)" : string.Join("\r\n", lines);
        }

        private static void AppendTimeClaim(List<string> lines, JObject payload, string claim, string label)
        {
            if (payload[claim] is not { } token || token.Type is not (JTokenType.Integer or JTokenType.Float)) return;
            var when = DateTimeOffset.FromUnixTimeSeconds((long)token);
            lines.Add($"{label}: {when:yyyy-MM-dd HH:mm:ss} UTC");
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
