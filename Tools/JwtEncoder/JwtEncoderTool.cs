using DevToolbox.Core;

namespace DevToolbox.Tools.JwtEncoder
{
    public class JwtEncoderTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "JWT Encoder";
        public string Description => "Builds and signs a compact JWT (HS256/384/512) from a JSON claims payload and a secret key.";

        /// <summary>Creates the JWT Encoder's editor/output control.</summary>
        public Control CreateView() => new JwtEncoderControl();
    }
}
