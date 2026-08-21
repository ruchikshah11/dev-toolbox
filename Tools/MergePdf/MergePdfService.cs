using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DevToolbox.Tools.MergePdf
{
    /// <summary>
    /// Combines multiple PDFs into one, in caller-supplied order, using PdfSharp's page-import
    /// mechanism (<see cref="PdfDocument.AddPage(PdfPage)"/> given a page from a document opened
    /// in <see cref="PdfDocumentOpenMode.Import"/> mode physically clones that page's content
    /// into the destination document - the same mechanism the PDF Password Remover's
    /// user-password fallback path already relies on).
    /// </summary>
    public static class MergePdfService
    {
        /// <summary>
        /// Opens every file in <paramref name="sourcePaths"/>, in order, and appends all of its
        /// pages onto one merged document written to <paramref name="destinationPath"/>.
        /// </summary>
        public static void Merge(IReadOnlyList<string> sourcePaths, string destinationPath)
        {
            if (sourcePaths is null || sourcePaths.Count == 0)
            {
                throw new ArgumentException("Choose at least one PDF file to merge.", nameof(sourcePaths));
            }

            var merged = new PdfDocument();
            foreach (var sourcePath in sourcePaths)
            {
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("A selected PDF file could not be found.", sourcePath);
                }

                using var imported = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                foreach (var page in imported.Pages)
                {
                    merged.AddPage(page);
                }
            }

            merged.Save(destinationPath);
        }
    }
}
