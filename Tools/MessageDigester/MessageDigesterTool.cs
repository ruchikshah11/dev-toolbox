using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.MessageDigester
{
    public class MessageDigesterTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Message Digester (MD5, SHA-256, SHA-512)";
        public string Description => "Computes MD5, SHA-256, or SHA-512 hashes of the given text.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to hash",
            "Digest (lowercase hex)",
            new[]
            {
                new TextTransformAction("MD5", MessageDigesterService.Md5),
                new TextTransformAction("SHA-256", MessageDigesterService.Sha256, Primary: true),
                new TextTransformAction("SHA-512", MessageDigesterService.Sha512)
            });
    }
}
