using PdfSharp.Pdf.IO;

namespace DevToolbox.Tools.RotatePdf
{
    /// <summary>
    /// Rotates one or more pages of a PDF by a multiple of 90 degrees, using PdfSharp's
    /// <see cref="PdfSharp.Pdf.PdfPage.Rotate"/> property - a viewer-applied /Rotate flag on the
    /// page dictionary, not a re-render of the page content, so this is lossless regardless of
    /// what the page contains.
    /// </summary>
    public static class RotatePdfService
    {
        /// <summary>
        /// Rotates <paramref name="pageNumbers"/> (1-based; null or empty means every page) in
        /// <paramref name="sourcePath"/> by <paramref name="degrees"/> (added to whatever rotation
        /// the page already has, normalized into 0/90/180/270) and saves the result to
        /// <paramref name="destinationPath"/>.
        /// </summary>
        public static void Rotate(string sourcePath, int degrees, IReadOnlyCollection<int>? pageNumbers, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            if (degrees % 90 != 0)
            {
                throw new ArgumentException("Rotation must be a multiple of 90 degrees.", nameof(degrees));
            }

            var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            var selected = pageNumbers is { Count: > 0 } ? new HashSet<int>(pageNumbers) : null;

            for (var i = 0; i < document.PageCount; i++)
            {
                var pageNumber = i + 1;
                if (selected is not null && !selected.Contains(pageNumber)) continue;

                var page = document.Pages[i];
                page.Rotate = ((page.Rotate + degrees) % 360 + 360) % 360;
            }

            document.Save(destinationPath);
        }

        /// <summary>
        /// Parses a page spec like "1,3,5-7" into the set of 1-based page numbers it names.
        /// Returns null for a blank/whitespace-only spec, meaning "every page". Throws
        /// <see cref="FormatException"/> for anything that isn't a comma-separated list of plain
        /// numbers and/or "a-b" ranges.
        /// </summary>
        public static HashSet<int>? ParsePageNumbers(string? spec)
        {
            if (string.IsNullOrWhiteSpace(spec)) return null;

            var result = new HashSet<int>();
            foreach (var rawPart in spec!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = rawPart.Trim();
                var dashIndex = part.IndexOf('-');
                if (dashIndex > 0)
                {
                    var startText = part.Substring(0, dashIndex).Trim();
                    var endText = part.Substring(dashIndex + 1).Trim();
                    if (!int.TryParse(startText, out var start) || !int.TryParse(endText, out var end) || end < start)
                    {
                        throw new FormatException($"'{part}' isn't a valid page range.");
                    }
                    for (var page = start; page <= end; page++) result.Add(page);
                }
                else if (int.TryParse(part, out var pageNumber))
                {
                    result.Add(pageNumber);
                }
                else
                {
                    throw new FormatException($"'{part}' isn't a valid page number.");
                }
            }
            return result;
        }
    }
}
