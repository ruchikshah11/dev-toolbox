using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DevToolbox.Tools.PdfToWord
{
    /// <summary>
    /// Converts a PDF to a .docx by extracting each page's text with PdfPig (using its
    /// content-order text extractor, which reconstructs word spacing and reading order - the
    /// raw per-character text PdfPig otherwise exposes runs every word together with no spaces
    /// at all) and writing the result as plain paragraphs via DocumentFormat.OpenXml. This is a
    /// text-extraction-based conversion, not a layout-reconstructing one: original fonts,
    /// columns, tables, and images are not recovered, and a PDF page that is a scanned image
    /// with no embedded text layer will extract as an empty page.
    /// </summary>
    public static class PdfToWordService
    {
        /// <summary>Reads <paramref name="sourcePdfPath"/> and writes its extracted text as a new .docx at <paramref name="destinationDocxPath"/>.</summary>
        public static void Convert(string sourcePdfPath, string destinationDocxPath)
        {
            if (!File.Exists(sourcePdfPath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePdfPath);
            }

            var pageTexts = ExtractPageTexts(sourcePdfPath);
            WriteDocx(pageTexts, destinationDocxPath);
        }

        /// <summary>Extracts each page's text in reading order, one string per PDF page.</summary>
        internal static List<string> ExtractPageTexts(string sourcePdfPath)
        {
            using var document = PdfDocument.Open(sourcePdfPath);

            var pageTexts = new List<string>();
            foreach (var page in document.GetPages())
            {
                // The `true` here tells PdfPig to insert line breaks between detected lines of
                // text (matching how the page visually wraps) rather than running the whole page
                // together as one unbroken string.
                pageTexts.Add(ContentOrderTextExtractor.GetText(page, true));
            }
            return pageTexts;
        }

        /// <summary>Writes each page's paragraphs into a new .docx, inserting a Word page break between PDF pages.</summary>
        private static void WriteDocx(List<string> pageTexts, string destinationDocxPath)
        {
            using var wordDocument = WordprocessingDocument.Create(destinationDocxPath, WordprocessingDocumentType.Document);
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            for (var pageIndex = 0; pageIndex < pageTexts.Count; pageIndex++)
            {
                var paragraphs = SplitIntoParagraphs(pageTexts[pageIndex]);
                if (paragraphs.Count == 0)
                {
                    // Keeps a blank/image-only PDF page from silently vanishing from the output.
                    body.AppendChild(new Paragraph());
                }

                foreach (var paragraphText in paragraphs)
                {
                    var text = new Text(paragraphText) { Space = SpaceProcessingModeValues.Preserve };
                    body.AppendChild(new Paragraph(new Run(text)));
                }

                if (pageIndex < pageTexts.Count - 1)
                {
                    body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
                }
            }

            mainPart.Document.Save();
        }

        /// <summary>
        /// Splits one page's extracted text into paragraphs on blank lines, joining any
        /// mid-paragraph line wraps (PdfPig's detected-line breaks, not real paragraph breaks)
        /// back into flowing single-line paragraph text.
        /// </summary>
        internal static List<string> SplitIntoParagraphs(string pageText)
        {
            if (pageText is not { Length: > 0 } || string.IsNullOrWhiteSpace(pageText))
            {
                return new List<string>();
            }

            var blocks = Regex.Split(pageText, @"\n\s*\n");
            var paragraphs = new List<string>();
            foreach (var block in blocks)
            {
                var joined = Regex.Replace(block, @"\s*\n\s*", " ").Trim();
                if (joined.Length > 0)
                {
                    paragraphs.Add(joined);
                }
            }
            return paragraphs;
        }
    }
}
