using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf.IO;

namespace DevToolbox.Tools.PdfPageNumbers
{
    /// <summary>Where on the page the page-number stamp is drawn.</summary>
    public enum PageNumberPosition
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        TopLeft,
        TopCenter,
        TopRight
    }

    /// <summary>
    /// Stamps a "Page X of N" label onto every page of a PDF using PdfSharp, drawing into each
    /// existing page's content stream in Append mode (<see cref="XGraphicsPdfPageOptions.Append"/>)
    /// so the page's original content is left intact underneath the stamp.
    /// </summary>
    public static class PdfPageNumbersService
    {
        private const string FontFamily = "Arial";
        private const double MarginPt = 24;

        /// <summary>
        /// Adds a "Page X of N" label to every page of <paramref name="sourcePath"/> at
        /// <paramref name="position"/> and saves the result to <paramref name="destinationPath"/>.
        /// </summary>
        public static void AddPageNumbers(string sourcePath, string destinationPath, PageNumberPosition position, double fontSizePt = 10)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            // See WordToPdfService - the netstandard2.0 build of PdfSharp 6.x needs this flag to
            // resolve fonts from the Windows Fonts folder; safe here since DevToolbox is
            // Windows-only.
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            var font = new XFont(FontFamily, fontSizePt, XFontStyleEx.Regular);
            var total = document.PageCount;

            for (var i = 0; i < total; i++)
            {
                var page = document.Pages[i];
                using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

                var text = $"Page {i + 1} of {total}";
                var size = gfx.MeasureString(text, font);
                var width = page.Width.Point;
                var height = page.Height.Point;

                var x = position switch
                {
                    PageNumberPosition.BottomLeft or PageNumberPosition.TopLeft => MarginPt,
                    PageNumberPosition.BottomCenter or PageNumberPosition.TopCenter => (width - size.Width) / 2,
                    _ => width - MarginPt - size.Width
                };
                var y = position is PageNumberPosition.TopLeft or PageNumberPosition.TopCenter or PageNumberPosition.TopRight
                    ? MarginPt + size.Height
                    : height - MarginPt;

                gfx.DrawString(text, font, XBrushes.Black, new XPoint(x, y));
            }

            document.Save(destinationPath);
        }
    }
}
