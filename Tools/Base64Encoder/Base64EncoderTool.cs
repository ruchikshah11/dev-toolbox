using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.Base64Encoder
{
    public class Base64EncoderTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Base 64 Encoder & Decoder";
        public string Description => "Base64-encodes text, or decodes a Base64 string back to text.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to Base64-encode, or a Base64 string to decode",
            "Result",
            new[]
            {
                new TextTransformAction("Base64 Encode", Base64EncoderService.Encode, Primary: true),
                new TextTransformAction("Base64 Decode", Base64EncoderService.Decode)
            });
    }
}
