using DevToolbox.Core;

namespace DevToolbox.Tools.JwtDecoder
{
    public class JwtDecoderTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "JWT Decoder";
        public string Description => "Decodes a JWT's header and payload, shows claim expiry, and optionally verifies HS256/384/512 signatures.";

        public Control CreateView() => new JwtDecoderControl();
    }
}
