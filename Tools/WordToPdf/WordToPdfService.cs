using System.Text.RegularExpressions;
using DevToolbox.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace DevToolbox.Tools.WordToPdf
{
    /// <summary>
    /// Converts a .docx to PDF by reading paragraph text, heading level, and basic run
    /// formatting (bold/italic/font size) via DocumentFormat.OpenXml, then laying that out into
    /// a new PDF with PdfSharp. This is a text/formatting-based conversion, not a pixel-perfect
    /// layout engine - tables, images, multi-column sections, headers/footers, and any other
    /// Word layout feature beyond plain paragraphs and headings are not reproduced.
    /// </summary>
    public static class WordToPdfService
    {
        private const string FontFamily = "Arial";
        private const double PageWidthPt = 612;   // US Letter
        private const double PageHeightPt = 792;
        private const double MarginLeft = 50;
        private const double MarginRight = 50;
        private const double MarginTop = 50;
        private const double MarginBottom = 50;
        private const double DefaultBodyFontSizePt = 11;
        private const double BlankLineHeightPt = 10;

        /// <summary>
        /// Reads <paramref name="sourceDocxPath"/> and writes a rendered PDF to
        /// <paramref name="destinationPdfPath"/>. When <paramref name="password"/> is non-empty,
        /// the saved PDF is encrypted with it (via <see cref="PdfEncryptionService"/>, the same
        /// PdfSharp security APIs the PDF Password Remover uses in the opposite direction) so it
        /// can't be reopened without that password; a null/empty password saves a plain,
        /// unencrypted PDF exactly as before.
        /// </summary>
        public static void Convert(string sourceDocxPath, string destinationPdfPath, string? password = null)
        {
            if (!File.Exists(sourceDocxPath))
            {
                throw new FileNotFoundException("The selected Word document could not be found.", sourceDocxPath);
            }

            var paragraphs = ExtractParagraphs(sourceDocxPath);
            RenderToPdf(paragraphs, destinationPdfPath, password);
        }

        /// <summary>Walks every top-level paragraph in the document body into a plain (text + formatting) model.</summary>
        internal static List<PdfParagraph> ExtractParagraphs(string sourceDocxPath)
        {
            using var wordDocument = WordprocessingDocument.Open(sourceDocxPath, false);
            var body = wordDocument.MainDocumentPart?.Document?.Body
                ?? throw new FormatException("This .docx file has no readable document body.");

            var paragraphs = new List<PdfParagraph>();
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                var headingLevel = GetHeadingLevel(paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value);
                var runs = ExtractRuns(paragraph);
                paragraphs.Add(new PdfParagraph(runs, headingLevel));
            }
            return paragraphs;
        }

        /// <summary>Extracts each run's text plus its bold/italic/font-size formatting, skipping empty runs.</summary>
        private static List<PdfRun> ExtractRuns(Paragraph paragraph)
        {
            var runs = new List<PdfRun>();
            foreach (var run in paragraph.Elements<Run>())
            {
                var text = string.Concat(run.Elements<Text>().Select(t => t.Text));
                if (text.Length == 0) continue;

                var properties = run.RunProperties;
                var isBold = properties?.Bold is { } bold && (bold.Val is null || bold.Val.Value);
                var isItalic = properties?.Italic is { } italic && (italic.Val is null || italic.Val.Value);

                // FontSize is stored in half-points (e.g. "24" = 12pt) - falls back to the
                // conventional 11pt body size when the run doesn't set one explicitly.
                double fontSizePt = DefaultBodyFontSizePt;
                if (properties?.FontSize?.Val?.Value is string sizeText && double.TryParse(sizeText, out var halfPoints))
                {
                    fontSizePt = halfPoints / 2.0;
                }

                runs.Add(new PdfRun(text, isBold, isItalic, fontSizePt));
            }
            return runs;
        }

        /// <summary>Maps a paragraph style id ("Heading1", "Title", ...) to a 0 (body text) - 3 heading level.</summary>
        private static int GetHeadingLevel(string? styleId)
        {
            if (styleId is not { Length: > 0 }) return 0;
            if (styleId.Equals("Title", StringComparison.OrdinalIgnoreCase)) return 1;

            var match = Regex.Match(styleId, @"^Heading(\d)$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var level))
            {
                return Math.Min(level, 3);
            }
            return 0;
        }

        /// <summary>
        /// Renders the extracted paragraphs into a fresh, multi-page-as-needed PDF document, then
        /// applies password protection (via <see cref="PdfEncryptionService"/>) before saving
        /// when <paramref name="password"/> is non-empty.
        /// </summary>
        private static void RenderToPdf(List<PdfParagraph> paragraphs, string destinationPath, string? password)
        {
            // The netstandard2.0 build of PdfSharp 6.x doesn't wire up its Windows font lookup
            // automatically (font resolution otherwise throws "No appropriate font found") -
            // this flag turns on reading real font files from the Windows Fonts folder, which is
            // safe here since DevToolbox only ever runs on Windows.
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var document = new PdfDocument();
            var page = CreatePage(document);
            var graphics = XGraphics.FromPdfPage(page);
            double y = MarginTop;
            const double contentWidth = PageWidthPt - MarginLeft - MarginRight;

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Runs.Count == 0)
                {
                    y += BlankLineHeightPt;
                    EnsureRoom(ref y, ref page, ref graphics, document, BlankLineHeightPt);
                    continue;
                }

                var isHeading = paragraph.HeadingLevel > 0;
                var headingSizePt = paragraph.HeadingLevel switch
                {
                    1 => 20.0,
                    2 => 16.0,
                    3 => 13.0,
                    _ => DefaultBodyFontSizePt
                };

                var tokens = BuildTokens(paragraph, isHeading, headingSizePt);
                y = DrawWrappedTokens(tokens, contentWidth, ref page, ref graphics, document, y);
                y += isHeading ? 10 : 6; // space after paragraph
                EnsureRoom(ref y, ref page, ref graphics, document, 0);
            }

            graphics.Dispose();

            if (password is { Length: > 0 })
            {
                PdfEncryptionService.ApplyPassword(document, password);
            }

            document.Save(destinationPath);
        }

        /// <summary>Splits every run's text into word/whitespace tokens, each carrying its own resolved XFont.</summary>
        private static List<(string Text, XFont Font)> BuildTokens(PdfParagraph paragraph, bool isHeading, double headingSizePt)
        {
            var tokens = new List<(string Text, XFont Font)>();
            foreach (var run in paragraph.Runs)
            {
                var style = XFontStyleEx.Regular;
                if (run.Bold || isHeading) style |= XFontStyleEx.Bold;
                if (run.Italic) style |= XFontStyleEx.Italic;

                var fontSizePt = isHeading ? headingSizePt : run.FontSizePt;
                var font = new XFont(FontFamily, fontSizePt, style);

                foreach (Match token in Regex.Matches(run.Text, @"\S+|\s+"))
                {
                    tokens.Add((token.Value, font));
                }
            }
            return tokens;
        }

        /// <summary>
        /// Lays out word-wrapped tokens left to right, wrapping to a new line (and, if needed, a
        /// new page) whenever a token would overflow <paramref name="contentWidth"/>. Returns the
        /// y position immediately below the last line drawn.
        /// </summary>
        private static double DrawWrappedTokens(List<(string Text, XFont Font)> tokens, double contentWidth,
            ref PdfPage page, ref XGraphics graphics, PdfDocument document, double y)
        {
            double x = MarginLeft;
            double lineHeight = 0;

            foreach (var (text, font) in tokens)
            {
                var isWhitespace = string.IsNullOrWhiteSpace(text);
                var size = graphics.MeasureString(text, font);

                if (!isWhitespace && x + size.Width > MarginLeft + contentWidth && x > MarginLeft)
                {
                    // Current line is full - drop to the next line, skipping the wrap-point's own
                    // leading whitespace token so lines don't start with a stray space.
                    x = MarginLeft;
                    y += lineHeight + 2;
                    lineHeight = 0;
                    EnsureRoom(ref y, ref page, ref graphics, document, font.Height);
                }

                if (!isWhitespace)
                {
                    graphics.DrawString(text, font, XBrushes.Black, new XPoint(x, y));
                }

                x += size.Width;
                lineHeight = Math.Max(lineHeight, font.Height);
            }

            return y + lineHeight;
        }

        /// <summary>Starts a fresh page (carrying the current XGraphics/page refs forward) once <paramref name="y"/> would run past the bottom margin.</summary>
        private static void EnsureRoom(ref double y, ref PdfPage page, ref XGraphics graphics, PdfDocument document, double neededHeight)
        {
            if (y + neededHeight <= PageHeightPt - MarginBottom) return;

            graphics.Dispose();
            page = CreatePage(document);
            graphics = XGraphics.FromPdfPage(page);
            y = MarginTop;
        }

        /// <summary>Adds a new US-Letter page to <paramref name="document"/>.</summary>
        private static PdfPage CreatePage(PdfDocument document)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(PageWidthPt);
            page.Height = XUnit.FromPoint(PageHeightPt);
            return page;
        }

        /// <summary>One extracted Word paragraph: its runs (each with its own formatting) plus its heading level (0 = body text).</summary>
        internal sealed record PdfParagraph(List<PdfRun> Runs, int HeadingLevel);

        /// <summary>One extracted Word run: its text plus bold/italic/font-size formatting.</summary>
        internal sealed record PdfRun(string Text, bool Bold, bool Italic, double FontSizePt);
    }
}
