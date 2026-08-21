using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace DevToolbox.Tools.CompressImage
{
    /// <summary>Preset JPEG re-encode quality, matching ilovepdf.com's 3-tier "compress" pattern.</summary>
    public enum ImageCompressionLevel
    {
        LowCompressionHighQuality = 85,
        MediumCompression = 60,
        HighCompressionLowQuality = 35
    }

    /// <summary>
    /// Compresses an image (JPEG, PNG, BMP, or anything else GDI+ can decode) by re-encoding it
    /// as a JPEG at a reduced quality, via <see cref="System.Drawing.Imaging"/> - no new NuGet
    /// dependency needed, since JPEG's Encoder.Quality parameter is already built into the .NET
    /// Framework's GDI+ wrapper.
    ///
    /// The output is always a JPEG, even for a PNG/GIF source, since that's where the actual
    /// lossy quality/size tradeoff (and the bulk of the size reduction) lives - PNG's own
    /// compression is lossless with no quality knob, so re-saving a PNG as a PNG would barely
    /// change its size. Because JPEG has no alpha channel, a source with transparency is
    /// flattened onto a white background first - genuinely lossy in that sense, and worth knowing
    /// before compressing a PNG you need to keep transparent.
    /// </summary>
    public static class CompressImageService
    {
        /// <summary>Before/after file size.</summary>
        public sealed record CompressionResult(long OriginalSizeBytes, long CompressedSizeBytes);

        /// <summary>
        /// Reads <paramref name="sourcePath"/>, flattens any transparency onto white, re-encodes
        /// as JPEG at <paramref name="level"/>'s quality, and saves it to
        /// <paramref name="destinationPath"/>.
        /// </summary>
        public static CompressionResult Compress(string sourcePath, string destinationPath, ImageCompressionLevel level)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected image file could not be found.", sourcePath);
            }

            var originalSize = new FileInfo(sourcePath).Length;

            using (var flattened = LoadFlattened(sourcePath))
            {
                var jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                using var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)level);
                flattened.Save(destinationPath, jpegCodec, encoderParams);
            }

            var compressedSize = new FileInfo(destinationPath).Length;
            return new CompressionResult(originalSize, compressedSize);
        }

        /// <summary>
        /// Loads the source image into an independent, alpha-free 24bpp bitmap - any transparency
        /// is composited onto a white background, since the JPEG output format has no alpha
        /// channel of its own.
        /// </summary>
        private static Bitmap LoadFlattened(string sourcePath)
        {
            var bytes = File.ReadAllBytes(sourcePath);

            Image source;
            try
            {
                using var ms = new MemoryStream(bytes);
                source = Image.FromStream(ms);
            }
            catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException)
            {
                // GDI+'s Image.FromStream famously throws OutOfMemoryException (not just
                // ArgumentException) for data that simply isn't a recognizable image format - a
                // long-standing GDI+ quirk, not an actual memory problem (see ImagePreviewerService).
                throw new FormatException("That file isn't a recognizable image format.", ex);
            }

            using (source)
            {
                var flattened = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
                using var graphics = Graphics.FromImage(flattened);
                graphics.Clear(Color.White);
                graphics.DrawImage(source, 0, 0, source.Width, source.Height);
                return flattened;
            }
        }
    }
}
