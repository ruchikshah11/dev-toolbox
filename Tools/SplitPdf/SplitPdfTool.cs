using DevToolbox.Core;

namespace DevToolbox.Tools.SplitPdf
{
    /// <summary>ITool registration for Split PDF.</summary>
    public class SplitPdfTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Split PDF";
        public string Description => "Splits a PDF - either extracts a page range into one new file, or breaks every page out into its own single-page file.";

        /// <summary>Creates the Split PDF's file-picker + range/per-page control.</summary>
        public Control CreateView() => new SplitPdfControl();
    }
}
