using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.UrlEncoder
{
    public class UrlEncoderTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Url Encoder & Decoder";
        public string Description => "URL-encodes or decodes text.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to URL-encode, or a URL-encoded string to decode",
            "Result",
            new[]
            {
                new TextTransformAction("URL Encode", UrlEncoderService.Encode, Primary: true),
                new TextTransformAction("URL Decode", UrlEncoderService.Decode)
            });
    }
}
