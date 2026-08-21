using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DevToolbox.Tools.SplitPdf
{
    /// <summary>
    /// Splits a PDF using PdfSharp's page-import mechanism (the same
    /// <see cref="PdfDocument.AddPage(PdfPage)"/>-from-an-Import-mode-source approach Merge PDFs
    /// and the PDF Password Remover's fallback path use) - either extracting one contiguous page
    /// range into a single output file, or writing every page out as its own single-page file.
    /// </summary>
    public static class SplitPdfService
    {
        /// <summary>Opens <paramref name="sourcePath"/> and returns its page count, without loading full page content - used to validate a requested range before extracting it.</summary>
        public static int GetPageCount(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            using var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
            return document.PageCount;
        }

        /// <summary>
        /// Extracts the 1-based, inclusive page range [<paramref name="firstPage"/>,
        /// <paramref name="lastPage"/>] from <paramref name="sourcePath"/> into one new PDF at
        /// <paramref name="destinationPath"/>.
        /// </summary>
        public static void ExtractRange(string sourcePath, int firstPage, int lastPage, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            using var imported = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
            var pageCount = imported.PageCount;
            if (firstPage < 1 || lastPage < firstPage || lastPage > pageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(firstPage),
                    $"Page range {firstPage}-{lastPage} is not valid for this {pageCount}-page document.");
            }

            var output = new PdfDocument();
            for (var i = firstPage; i <= lastPage; i++)
            {
                output.AddPage(imported.Pages[i - 1]);
            }

            output.Save(destinationPath);
        }

        /// <summary>
        /// Writes every page of <paramref name="sourcePath"/> out as its own single-page PDF
        /// inside <paramref name="outputFolder"/> (created if missing), named
        /// "&lt;source-name&gt;-page-&lt;n&gt;.pdf" with the page number zero-padded to the
        /// document's total page count's digit width. Returns the full paths written, in page
        /// order.
        /// </summary>
        public static List<string> SplitEveryPage(string sourcePath, string outputFolder)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            Directory.CreateDirectory(outputFolder);
            var baseName = Path.GetFileNameWithoutExtension(sourcePath);
            var written = new List<string>();

            using var imported = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
            var digits = imported.PageCount.ToString().Length;
            for (var i = 0; i < imported.PageCount; i++)
            {
                var output = new PdfDocument();
                output.AddPage(imported.Pages[i]);

                var fileName = $"{baseName}-page-{(i + 1).ToString().PadLeft(digits, '0')}.pdf";
                var fullPath = Path.Combine(outputFolder, fileName);
                output.Save(fullPath);
                written.Add(fullPath);
            }

            return written;
        }
    }
}
