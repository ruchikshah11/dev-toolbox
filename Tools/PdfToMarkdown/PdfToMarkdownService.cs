using DevToolbox.Tools.PdfToWord;

namespace DevToolbox.Tools.PdfToMarkdown
{
    /// <summary>
    /// Converts a PDF to a .md file, reusing PdfToWordService's PdfPig-based text extraction
    /// (content-order extraction plus blank-line paragraph splitting) rather than re-implementing
    /// it - the two tools differ only in what they do with the extracted paragraphs (write a
    /// .docx vs. write Markdown text). Same scope/limitations as PDF to Word: this is a plain
    /// text-extraction conversion, not a layout-reconstructing one - no heading detection, no
    /// tables/images, and a scanned (image-only) page extracts as empty.
    /// </summary>
    public static class PdfToMarkdownService
    {
        /// <summary>Reads <paramref name="sourcePdfPath"/> and writes its extracted text as Markdown at <paramref name="destinationMarkdownPath"/>.</summary>
        public static void Convert(string sourcePdfPath, string destinationMarkdownPath)
        {
            if (!File.Exists(sourcePdfPath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePdfPath);
            }

            var pageTexts = PdfToWordService.ExtractPageTexts(sourcePdfPath);
            File.WriteAllText(destinationMarkdownPath, BuildMarkdown(pageTexts));
        }

        /// <summary>Joins each page's paragraphs with blank lines (a Markdown paragraph break), and separates pages with a horizontal rule.</summary>
        internal static string BuildMarkdown(List<string> pageTexts)
        {
            var pageBlocks = new List<string>();
            foreach (var pageText in pageTexts)
            {
                var paragraphs = PdfToWordService.SplitIntoParagraphs(pageText);
                pageBlocks.Add(string.Join("\n\n", paragraphs));
            }

            // "---" is the standard Markdown horizontal rule - used here purely as a visual page
            // separator, not as any semantic heading/section marker.
            return string.Join("\n\n---\n\n", pageBlocks);
        }
    }
}
