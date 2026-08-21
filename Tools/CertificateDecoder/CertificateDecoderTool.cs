using DevToolbox.Core;

namespace DevToolbox.Tools.CertificateDecoder
{
    public class CertificateDecoderTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Certificate Decoder";
        public string Description => "Decodes an X.509 certificate (PEM, base64 DER, or an uploaded .cer/.crt/.pem file) and shows subject, issuer, validity dates, and thumbprint.";

        /// <summary>Creates the Certificate Decoder's paste/upload + output control.</summary>
        public Control CreateView() => new CertificateDecoderControl();
    }
}
