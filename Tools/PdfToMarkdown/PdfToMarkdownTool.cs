using DevToolbox.Core;

namespace DevToolbox.Tools.PdfToMarkdown
{
    /// <summary>ITool registration for the PDF to Markdown converter.</summary>
    public class PdfToMarkdownTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "PDF to Markdown";
        public string Description => "Extracts a PDF's page text into a new .md file as plain paragraphs. Fonts, headings, columns, tables, images, and scanned/image-only pages are not preserved.";

        /// <summary>Creates the PDF to Markdown's file-picker + convert + save control.</summary>
        public Control CreateView() => new PdfToMarkdownControl();
    }
}
