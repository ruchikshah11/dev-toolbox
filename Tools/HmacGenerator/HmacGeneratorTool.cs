using DevToolbox.Core;

namespace DevToolbox.Tools.HmacGenerator
{
    public class HmacGeneratorTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "HMAC Generator";
        public string Description => "Computes an HMAC (MD5/SHA1/SHA256/SHA512) of a message using a secret key.";

        public Control CreateView() => new HmacGeneratorControl();
    }
}
