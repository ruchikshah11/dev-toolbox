using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace DevToolbox.Tools.CertificateDecoder
{
    public readonly record struct CertificateInfo(
        string Subject,
        string Issuer,
        string SerialNumber,
        string Thumbprint,
        string SignatureAlgorithm,
        string PublicKeyAlgorithm,
        int KeySize,
        string Version,
        DateTime NotBeforeUtc,
        DateTime NotAfterUtc,
        bool HasPrivateKey);

    public static class CertificateDecoderService
    {
        /// <summary>Decodes a certificate from pasted PEM text or a base64-encoded DER body.</summary>
        public static CertificateInfo Decode(string pastedText) => Decode(ExtractBytes(pastedText));

        /// <summary>Decodes a certificate directly from raw file bytes (DER or PEM-as-text).</summary>
        public static CertificateInfo Decode(byte[] rawBytes) => BuildInfo(LoadCertificate(rawBytes));

        /// <summary>Renders a <see cref="CertificateInfo"/> as the multi-line summary shown in the output box.</summary>
        public static string FormatSummary(CertificateInfo info)
        {
            var now = DateTime.UtcNow;
            var status = now < info.NotBeforeUtc ? "NOT YET VALID"
                : now > info.NotAfterUtc ? "EXPIRED"
                : "Valid";

            var keySizeSuffix = info.KeySize > 0 ? $" ({info.KeySize}-bit)" : string.Empty;

            return string.Join("\r\n", new[]
            {
                $"Subject: {info.Subject}",
                $"Issuer: {info.Issuer}",
                $"Serial Number: {info.SerialNumber}",
                $"Thumbprint (SHA1): {info.Thumbprint}",
                $"Signature Algorithm: {info.SignatureAlgorithm}",
                $"Public Key Algorithm: {info.PublicKeyAlgorithm}{keySizeSuffix}",
                $"Version: {info.Version}",
                $"Not Before: {info.NotBeforeUtc:yyyy-MM-dd HH:mm:ss} UTC",
                $"Not After: {info.NotAfterUtc:yyyy-MM-dd HH:mm:ss} UTC",
                $"Has Private Key: {info.HasPrivateKey}",
                $"Status: {status}"
            });
        }

        /// <summary>Parses raw bytes (DER or PEM text) into an X509Certificate2, surfacing failures as FormatException.</summary>
        private static X509Certificate2 LoadCertificate(byte[] bytes)
        {
            try
            {
                return new X509Certificate2(bytes);
            }
            catch (CryptographicException ex)
            {
                throw new FormatException($"Could not parse a certificate from the input: {ex.Message}", ex);
            }
        }

        /// <summary>Projects an X509Certificate2's fields into the plain CertificateInfo the UI displays.</summary>
        private static CertificateInfo BuildInfo(X509Certificate2 cert)
        {
            int keySize;
            try
            {
                // AsymmetricAlgorithm.KeySize throws for key types this API can't materialize
                // (some ECC curves on older CNG providers) - fall back to "unknown" rather than
                // failing the whole decode over a cosmetic detail.
                keySize = cert.PublicKey.Key.KeySize;
            }
            catch (NotSupportedException)
            {
                keySize = 0;
            }

            return new CertificateInfo(
                cert.Subject,
                cert.Issuer,
                cert.SerialNumber,
                cert.Thumbprint,
                cert.SignatureAlgorithm.FriendlyName ?? cert.SignatureAlgorithm.Value ?? "(unknown)",
                cert.PublicKey.Oid.FriendlyName ?? cert.PublicKey.Oid.Value ?? "(unknown)",
                keySize,
                $"V{cert.Version}",
                cert.NotBefore.ToUniversalTime(),
                cert.NotAfter.ToUniversalTime(),
                cert.HasPrivateKey);
        }

        // Accepts PEM (with -----BEGIN CERTIFICATE----- headers) or raw base64-encoded DER -
        // strips whitespace/header lines and base64-decodes down to the DER bytes X509Certificate2
        // needs.
        private static byte[] ExtractBytes(string input)
        {
            input = (input ?? string.Empty).Trim();
            if (input.Length == 0) throw new FormatException("Paste a certificate (PEM or base64-encoded DER) to decode.");

            if (input.Contains("-----BEGIN"))
            {
                var lines = input.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0 && !l.StartsWith("-----"));
                input = string.Join(string.Empty, lines);
            }
            else
            {
                input = Regex.Replace(input, @"\s+", string.Empty);
            }

            try
            {
                return Convert.FromBase64String(input);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Could not base64-decode the certificate body - check it's a valid PEM or base64 DER certificate.", ex);
            }
        }
    }
}
