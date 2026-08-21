using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevToolbox.Tools.JwtEncoder
{
    public static class JwtEncoderService
    {
        public static readonly string[] Algorithms = { "HS256", "HS384", "HS512" };

        /// <summary>Builds and signs a compact JWT from a JSON claims payload using an HS256/384/512 secret.</summary>
        public static string Encode(string payloadJson, string secret, string algorithm)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) throw new FormatException("Enter the JWT payload as JSON.");
            if (string.IsNullOrEmpty(secret)) throw new FormatException("Enter a secret key to sign with.");

            JObject payload;
            try
            {
                payload = JObject.Parse(payloadJson);
            }
            catch (JsonReaderException ex)
            {
                throw new FormatException($"Payload is not valid JSON: {ex.Message}", ex);
            }

            var header = new JObject { ["alg"] = algorithm, ["typ"] = "JWT" };
            var headerSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(header.ToString(Formatting.None)));
            var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payload.ToString(Formatting.None)));

            var signingInput = Encoding.UTF8.GetBytes($"{headerSegment}.{payloadSegment}");
            var signatureSegment = Base64UrlEncode(Sign(signingInput, secret, algorithm));

            return $"{headerSegment}.{payloadSegment}.{signatureSegment}";
        }

        /// <summary>Computes the HMAC signature bytes for the given algorithm name.</summary>
        private static byte[] Sign(byte[] signingInput, string secret, string algorithm)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            using HMAC hmac = algorithm switch
            {
                "HS256" => new HMACSHA256(keyBytes),
                "HS384" => new HMACSHA384(keyBytes),
                "HS512" => new HMACSHA512(keyBytes),
                _ => throw new ArgumentException($"Unknown algorithm '{algorithm}'.", nameof(algorithm))
            };
            return hmac.ComputeHash(signingInput);
        }

        /// <summary>Base64url-encodes (no padding, URL-safe alphabet) per the JWT spec.</summary>
        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
