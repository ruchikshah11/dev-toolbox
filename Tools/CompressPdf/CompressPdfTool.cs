using DevToolbox.Core;

namespace DevToolbox.Tools.CompressPdf
{
    /// <summary>ITool registration for Compress PDF.</summary>
    public class CompressPdfTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Compress PDF";

        public string Description =>
            "Shrinks a PDF by re-encoding its embedded JPEG images at a lower quality. Only helps PDFs that "
            + "contain embedded photos/raster images - a pure vector/text PDF, or one whose images are already "
            + "highly compressed, will shrink little or not at all.";

        /// <summary>Creates the Compress PDF's file-picker + quality-preset + save control.</summary>
        public Control CreateView() => new CompressPdfControl();
    }
}
