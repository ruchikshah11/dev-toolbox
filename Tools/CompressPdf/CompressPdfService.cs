using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DevToolbox.Tools.CompressPdf
{
    /// <summary>Preset JPEG re-encode quality, matching ilovepdf.com's 3-tier "compress" pattern.</summary>
    public enum PdfCompressionLevel
    {
        LowCompressionHighQuality = 85,
        MediumCompression = 60,
        HighCompressionLowQuality = 35
    }

    /// <summary>
    /// Shrinks a PDF by re-encoding its embedded JPEG (DCTDecode-filtered) images at a lower
    /// quality, then re-saving the document (which also gets PdfSharp's normal stream
    /// compression for free). PdfSharp itself has no built-in "compress this PDF" operation - the
    /// real size win here comes from walking each page's XObject resources, finding Image
    /// XObjects, decoding the raster data with <see cref="Bitmap"/>, and re-encoding via GDI+'s
    /// JPEG encoder at the chosen quality, replacing the XObject's stream bytes in place.
    ///
    /// This is honestly scoped, not universal: only images whose PDF /Filter is /DCTDecode (i.e.
    /// already JPEG-compressed) are touched - that covers the vast majority of embedded photos in
    /// real-world PDFs, but a page with no embedded raster images (pure vector/text) will shrink
    /// little or not at all, an already low-quality/optimized JPEG won't shrink much further, and
    /// images using other filters (e.g. raw FlateDecode bitmap data, or CMYK JPEGs GDI+ can't
    /// decode) are left untouched rather than risking a corrupted image.
    /// </summary>
    public static class CompressPdfService
    {
        /// <summary>Before/after file size and how many embedded images were actually recompressed vs. left untouched.</summary>
        public sealed record CompressionResult(long OriginalSizeBytes, long CompressedSizeBytes, int ImagesRecompressed, int ImagesSkipped);

        /// <summary>
        /// Recompresses every DCTDecode image XObject (directly on a page, or nested inside a
        /// Form XObject) in <paramref name="sourcePath"/> at <paramref name="level"/>'s JPEG
        /// quality and saves the result to <paramref name="destinationPath"/>.
        /// </summary>
        public static CompressionResult Compress(string sourcePath, string destinationPath, PdfCompressionLevel level)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            var originalSize = new FileInfo(sourcePath).Length;
            var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);

            var recompressed = 0;
            var skipped = 0;
            foreach (var page in document.Pages)
            {
                var xObjects = page.Resources?.Elements.GetDictionary("/XObject");
                if (xObjects is not null)
                {
                    ProcessXObjects(xObjects, (int)level, ref recompressed, ref skipped, depth: 0);
                }
            }

            document.Save(destinationPath);
            var compressedSize = new FileInfo(destinationPath).Length;
            return new CompressionResult(originalSize, compressedSize, recompressed, skipped);
        }

        /// <summary>Walks one /XObject dictionary, recompressing images and recursing into nested Form XObjects' own resources (depth-limited against pathological nesting).</summary>
        private static void ProcessXObjects(PdfDictionary xObjectsDict, int quality, ref int recompressed, ref int skipped, int depth)
        {
            if (depth > 4) return;

            foreach (var key in xObjectsDict.Elements.Keys.ToList())
            {
                var xObject = xObjectsDict.Elements.GetDictionary(key);
                if (xObject is null) continue;

                var subtype = xObject.Elements.GetName("/Subtype");
                if (subtype == "/Image")
                {
                    if (TryRecompressImage(xObject, quality)) recompressed++;
                    else skipped++;
                }
                else if (subtype == "/Form")
                {
                    var nested = xObject.Elements.GetDictionary("/Resources")?.Elements.GetDictionary("/XObject");
                    if (nested is not null)
                    {
                        ProcessXObjects(nested, quality, ref recompressed, ref skipped, depth + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Re-encodes one image XObject's raw stream bytes as JPEG at <paramref name="quality"/>
        /// (0-100) and replaces its stream in place. Returns false (leaving the image untouched)
        /// for anything that isn't a plain /DCTDecode stream, that GDI+ can't decode (e.g. a CMYK
        /// JPEG), or where re-encoding didn't actually come out smaller.
        /// </summary>
        private static bool TryRecompressImage(PdfDictionary imageXObject, int quality)
        {
            if (imageXObject.Elements.GetName("/Filter") != "/DCTDecode") return false;

            try
            {
                // Per PdfStream.Value's own docs, this returns the bytes "as they are" - for a
                // /DCTDecode-filtered image, that's already the raw JPEG byte stream, directly
                // loadable by GDI+ with no separate PDF-level decoding step.
                var originalBytes = imageXObject.Stream.Value;

                using var inputStream = new MemoryStream(originalBytes);
                using var bitmap = new Bitmap(inputStream);
                using var outputStream = new MemoryStream();

                var jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                using var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                bitmap.Save(outputStream, jpegCodec, encoderParams);

                var newBytes = outputStream.ToArray();
                if (newBytes.Length >= originalBytes.Length) return false;

                imageXObject.Stream.Value = newBytes;
                imageXObject.Elements.SetInteger("/Length", newBytes.Length);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException or IOException)
            {
                // GDI+ throws OutOfMemoryException (not just ArgumentException) for image data it
                // can't decode (e.g. CMYK JPEGs) - a long-standing GDI+ quirk, not an actual
                // memory problem. Leave that image exactly as it was rather than risk corrupting it.
                return false;
            }
        }
    }
}
