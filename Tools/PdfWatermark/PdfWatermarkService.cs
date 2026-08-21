using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;

namespace DevToolbox.Tools.PdfWatermark
{
    /// <summary>
    /// Adds a diagonal, semi-transparent text watermark to every page of a PDF, and offers a
    /// best-effort watermark *removal* that searches each page's parsed content stream for text
    /// matching a given string and strips just those text-drawing operations.
    ///
    /// Removal is genuinely real (it edits the actual PDF content stream via PdfSharp's own
    /// content-stream parser, <see cref="ContentReader"/>/<see cref="CSequence"/>, not a
    /// cosmetic overlay) but it is NOT universal: it only finds watermark text that appears as a
    /// literal string in a Tj/'/"/TJ text-show operator matching the search text byte-for-byte
    /// (case-insensitive). That covers watermarks stamped with a simple, non-subsetted font and
    /// standard encoding (including anything this tool's own Add Watermark step produces). It
    /// will NOT find a watermark that's actually a raster image, or text drawn with a subsetted/
    /// custom-encoded embedded font (common in PDFs exported from Word/PowerPoint/Acrobat)
    /// whose character codes don't map back to the same characters as the search text.
    /// </summary>
    public static class PdfWatermarkService
    {
        private const string FontFamily = "Arial";

        /// <summary>
        /// Stamps <paramref name="text"/> diagonally across every page of
        /// <paramref name="sourcePath"/> - semi-transparent gray, rotated
        /// <paramref name="rotationDegrees"/> degrees (negative = counter-clockwise) about each
        /// page's center, at <paramref name="opacityPercent"/>% opacity (1-100) - and saves the
        /// result to <paramref name="destinationPath"/>.
        /// </summary>
        public static void AddWatermark(string sourcePath, string destinationPath, string text,
            int opacityPercent = 30, double rotationDegrees = -45, double fontSizePt = 54)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Enter the watermark text.", nameof(text));
            }

            // See WordToPdfService - the netstandard2.0 build of PdfSharp 6.x needs this flag to
            // resolve fonts from the Windows Fonts folder; safe here since DevToolbox is
            // Windows-only.
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            var font = new XFont(FontFamily, fontSizePt, XFontStyleEx.Bold);
            var clampedOpacity = Math.Max(1, Math.Min(100, opacityPercent));
            var alpha = clampedOpacity * 255 / 100;
            var brush = new XSolidBrush(XColor.FromArgb(alpha, 128, 128, 128));

            foreach (var page in document.Pages)
            {
                using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                gfx.TranslateTransform(page.Width.Point / 2, page.Height.Point / 2);
                gfx.RotateTransform(rotationDegrees);
                gfx.DrawString(text, font, brush, new XPoint(0, 0), XStringFormats.Center);
            }

            document.Save(destinationPath);
        }

        /// <summary>
        /// Best-effort watermark removal - see the type-level remarks for exactly what this can
        /// and can't find. Strips any Tj/'/"/TJ text-show operator whose string operand(s)
        /// contain <paramref name="searchText"/> (case-insensitive) from every page's content
        /// stream, then saves the result to <paramref name="destinationPath"/>. Returns the
        /// number of text-show operators removed, across the whole document, so the caller can
        /// tell the user when nothing matched.
        /// </summary>
        public static int RemoveWatermarkText(string sourcePath, string destinationPath, string searchText)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException("Enter the watermark text to search for.", nameof(searchText));
            }

            var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            var totalRemoved = 0;

            foreach (var page in document.Pages)
            {
                var sequence = ContentReader.ReadContent(page);
                var newSequence = new CSequence();

                foreach (var obj in sequence)
                {
                    if (obj is COperator op && op.Name is "Tj" or "'" or "\"")
                    {
                        if (OperandsContain(op.Operands, searchText))
                        {
                            totalRemoved++;
                            continue;
                        }
                    }
                    else if (obj is COperator tj && tj.Name == "TJ")
                    {
                        var array = tj.Operands.OfType<CArray>().FirstOrDefault();
                        if (array is not null)
                        {
                            for (var i = array.Count - 1; i >= 0; i--)
                            {
                                if (array[i] is CString element && Matches(element.Value, searchText))
                                {
                                    array.RemoveAt(i);
                                }
                            }
                            if (array.Count == 0)
                            {
                                totalRemoved++;
                                continue;
                            }
                        }
                    }

                    newSequence.Add(obj);
                }

                page.Contents.ReplaceContent(newSequence);
            }

            document.Save(destinationPath);
            return totalRemoved;
        }

        /// <summary>True if any string operand in <paramref name="operands"/> contains <paramref name="searchText"/> (case-insensitive).</summary>
        private static bool OperandsContain(CSequence operands, string searchText) =>
            operands.OfType<CString>().Any(s => Matches(s.Value, searchText));

        private static bool Matches(string value, string searchText) =>
            value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
