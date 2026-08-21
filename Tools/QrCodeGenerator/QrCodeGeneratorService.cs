using QRCoder;

namespace DevToolbox.Tools.QrCodeGenerator
{
    public static class QrCodeGeneratorService
    {
        // Level Q (~25% error correction) is a reasonable default for on-screen + printed codes,
        // matching QRCoder's own samples.
        public static byte[] GeneratePng(string text)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text ?? string.Empty, QRCodeGenerator.ECCLevel.Q);
            using var pngCode = new PngByteQRCode(data);
            return pngCode.GetGraphic(20);
        }
    }
}
