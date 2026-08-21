using DevToolbox.Core;

namespace DevToolbox.Tools.QrCodeGenerator
{
    public class QrCodeGeneratorTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "QR Code Generator";
        public string Description => "Generates a QR code image from text or a URL.";

        public Control CreateView() => new QrCodeGeneratorControl();
    }
}
