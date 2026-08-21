using DevToolbox.Core;

namespace DevToolbox.Tools.CompressImage
{
    /// <summary>ITool registration for Compress Image.</summary>
    public class CompressImageTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Compress Image";

        public string Description =>
            "Shrinks an image (JPEG, PNG, BMP, ...) by re-encoding it as a JPEG at a reduced quality. "
            + "Always outputs JPEG - a transparent PNG is flattened onto white first, since JPEG has no alpha channel.";

        /// <summary>Creates the Compress Image's file-picker + quality-preset + save control.</summary>
        public Control CreateView() => new CompressImageControl();
    }
}
