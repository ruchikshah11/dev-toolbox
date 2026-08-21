using DevToolbox.Core;

namespace DevToolbox.Tools.MergePdf
{
    /// <summary>ITool registration for Merge PDFs.</summary>
    public class MergePdfTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Merge PDFs";
        public string Description => "Combines multiple PDF files into one, in an order you control, and saves the result as a new PDF.";

        /// <summary>Creates the Merge PDFs' multi-file-picker + reorder + save control.</summary>
        public Control CreateView() => new MergePdfControl();
    }
}
