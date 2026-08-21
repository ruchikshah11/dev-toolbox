using DevToolbox.Core;

namespace DevToolbox.Tools.PdfWatermark
{
    /// <summary>ITool registration for Add/Remove Watermark.</summary>
    public class PdfWatermarkTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Add/Remove Watermark";

        public string Description =>
            "Stamps a diagonal, semi-transparent text watermark onto every page of a PDF. Also offers "
            + "best-effort watermark removal, which searches each page's content stream for text matching "
            + "a string you provide and strips just that text - this reliably removes plain, non-subsetted-font "
            + "text watermarks (including ones this tool adds), but can't remove an image-based watermark or "
            + "text drawn with a subsetted/custom-encoded embedded font.";

        /// <summary>Creates the Add/Remove Watermark's file-picker + mode control.</summary>
        public Control CreateView() => new PdfWatermarkControl();
    }
}
