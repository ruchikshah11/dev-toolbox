using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.Base32Encoder
{
    public class Base32EncoderTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Base 32 Encoder & Decoder";
        public string Description => "Base32-encodes text (RFC 4648), or decodes a Base32 string back to text.";

        /// <summary>Wires the Base32 encode/decode actions into the shared paste-in/run/see-result shell.</summary>
        public Control CreateView() => new TextTransformControl(
            "Enter the text to Base32-encode, or a Base32 string to decode",
            "Result",
            new[]
            {
                new TextTransformAction("Base32 Encode", Base32EncoderService.Encode, Primary: true),
                new TextTransformAction("Base32 Decode", Base32EncoderService.Decode)
            });
    }
}
