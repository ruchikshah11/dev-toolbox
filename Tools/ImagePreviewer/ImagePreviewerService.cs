using System.Drawing;
using System.Text.RegularExpressions;

namespace DevToolbox.Tools.ImagePreviewer
{
    public readonly record struct DecodedImagePreview(Bitmap Image, string MimeType, int WidthPx, int HeightPx, int ByteCount, string Base64Only);

    public static class ImagePreviewerService
    {
        /// <summary>Decodes a "data:&lt;mime&gt;;base64,..." URI or a bare base64 string into a previewable image.</summary>
        public static DecodedImagePreview Decode(string input)
        {
            input = (input ?? string.Empty).Trim();
            if (input.Length == 0) throw new FormatException("Paste a data URI (data:image/png;base64,...) or a bare base64 image string.");

            string base64;
            string mimeType;

            if (input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = input.IndexOf(',');
                if (commaIndex < 0) throw new FormatException("Malformed data URI - expected a comma separating the header from the data.");

                var header = input.Substring(5, commaIndex - 5);
                if (header.IndexOf("base64", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new FormatException("Only base64-encoded data URIs are supported (missing \";base64\").");

                var semiIndex = header.IndexOf(';');
                mimeType = semiIndex >= 0 ? header.Substring(0, semiIndex) : "image";
                base64 = input.Substring(commaIndex + 1);
            }
            else
            {
                base64 = input;
                mimeType = "(unspecified - bare base64)";
            }

            base64 = Regex.Replace(base64, @"\s+", string.Empty);

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Could not base64-decode the image data.", ex);
            }

            Bitmap bitmap;
            try
            {
                using var ms = new MemoryStream(bytes);
                using var source = Image.FromStream(ms);
                // Copy into an independent Bitmap rather than keeping the Image tied to `ms` -
                // GDI+ can lazily re-read from the source stream, which would be a
                // use-after-dispose once this method's `using` returns (same pattern as
                // CategoryIcons' SharePoint logo loading).
                bitmap = new Bitmap(source);
            }
            catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException)
            {
                // GDI+'s Image.FromStream famously throws OutOfMemoryException (not just
                // ArgumentException) for data that simply isn't a recognizable image format -
                // a long-standing GDI+ quirk, not an actual memory problem.
                throw new FormatException("Decoded bytes are not a recognizable image format.", ex);
            }

            return new DecodedImagePreview(bitmap, mimeType, bitmap.Width, bitmap.Height, bytes.Length, base64);
        }

        /// <summary>Reads an image file from disk and builds its "data:&lt;mime&gt;;base64,..." URI.</summary>
        public static string EncodeFileToDataUri(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var mimeType = GuessMimeType(Path.GetExtension(filePath));
            return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        }

        private static string GuessMimeType(string extension) => extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };
    }
}
