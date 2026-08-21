using DevToolbox.Core;

namespace DevToolbox.Tools.PdfPageNumbers
{
    /// <summary>ITool registration for Add Page Numbers.</summary>
    public class PdfPageNumbersTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Add Page Numbers";
        public string Description => "Stamps a \"Page X of N\" label onto every page of a PDF, at a position you choose, and saves the result.";

        /// <summary>Creates the Add Page Numbers' file-picker + position + save control.</summary>
        public Control CreateView() => new PdfPageNumbersControl();
    }
}
