using DevToolbox.Core;

namespace DevToolbox.Tools.PdfToWord
{
    /// <summary>ITool registration for the PDF to Word converter.</summary>
    public class PdfToWordTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "PDF to Word";
        public string Description => "Extracts a PDF's page text into a new .docx as plain paragraphs. Fonts, columns, tables, images, and scanned/image-only pages are not preserved.";

        /// <summary>Creates the PDF to Word's file-picker + convert + save control.</summary>
        public Control CreateView() => new PdfToWordControl();
    }
}
