using System.Drawing.Drawing2D;

namespace DevToolbox.UI
{
    // Nav category header icons. Every real category (everything but the "Pinned" pseudo-category
    // at the top) uses a colorful image rather than hand-drawn monochrome line art - each is a
    // clean 256x256 render (via resvg) of a Microsoft Fluent UI Emoji "Flat" style SVG
    // (github.com/microsoft/fluentui-emoji, MIT licensed), chosen to evoke that category's
    // purpose (a key for Encoders/Cryptography, a checkmark for Validators, ...). SharePoint uses
    // the real product logo instead, for the same reason every other category uses a themed emoji
    // rather than a generic glyph - recognizability at a glance.
    internal static class CategoryIcons
    {
        private static readonly Dictionary<string, string> ImageResourceNames = new()
        {
            ["Formatters"] = "DevToolbox.Assets.Formatters.png",
            ["Validators"] = "DevToolbox.Assets.Validators.png",
            ["Converters"] = "DevToolbox.Assets.Converters.png",
            ["Encoders / Cryptography"] = "DevToolbox.Assets.EncodersCryptography.png",
            ["Code Minifiers / Beautifier"] = "DevToolbox.Assets.CodeMinifiersBeautifier.png",
            ["String Escaper & Utilities"] = "DevToolbox.Assets.StringEscaperUtilities.png",
            ["Web Resources"] = "DevToolbox.Assets.WebResources.png",
            ["SharePoint"] = "DevToolbox.Assets.SharePoint.png",
            ["PDF Tools"] = "DevToolbox.Assets.PdfTools.png",
            ["Code Runner"] = "DevToolbox.Assets.CodeRunner.png",
        };

        // Loaded once per category and cached - GetManifestResourceStream/Image.FromStream on
        // every paint would be wasteful for a nav that redraws on every hover/selection change.
        private static readonly Dictionary<string, Lazy<Image?>> LoadedImages =
            ImageResourceNames.ToDictionary(kv => kv.Key, kv => new Lazy<Image?>(() => LoadImage(kv.Value)));

        private static Image? LoadImage(string resourceName)
        {
            var assembly = typeof(CategoryIcons).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) return null;

            // Decode into an independent Bitmap rather than returning the Image tied to
            // `stream` directly - GDI+ can lazily re-read from the source stream, which would
            // be a use-after-dispose once this method's `using` returns.
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }

        public static void Draw(Graphics g, string category, Rectangle b, Color color)
        {
            if (category == "Pinned")
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(color);
                DrawStar(g, brush, b);
                return;
            }

            if (category == "Recently Used")
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                DrawClock(g, pen, b);
                return;
            }

            if (!LoadedImages.TryGetValue(category, out var lazyImage)) return;
            var image = lazyImage.Value;
            if (image is null) return;

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var scale = Math.Min((float)b.Width / image.Width, (float)b.Height / image.Height);
            var w = image.Width * scale;
            var h = image.Height * scale;
            var x = b.Left + (b.Width - w) / 2f;
            var y = b.Top + (b.Height - h) / 2f;
            g.DrawImage(image, x, y, w, h);
        }

        // A 5-point star for the "Pinned" pseudo-category at the top of the nav - kept as a
        // themed-color monochrome glyph rather than an image, since "pinned" isn't a real tool
        // category with its own icon to borrow.
        private static void DrawStar(Graphics g, Brush brush, Rectangle b)
        {
            var cx = b.Left + b.Width / 2f;
            var cy = b.Top + b.Height / 2f;
            var outerR = Math.Min(b.Width, b.Height) / 2f;
            var innerR = outerR * 0.42f;

            var points = new PointF[10];
            for (var i = 0; i < 10; i++)
            {
                var r = i % 2 == 0 ? outerR : innerR;
                var angle = (float)(Math.PI / 2 * 3) + i * (float)Math.PI / 5;
                points[i] = new PointF(cx + r * (float)Math.Cos(angle), cy + r * (float)Math.Sin(angle));
            }
            g.FillPolygon(brush, points);
        }

        // A clock face (circle + hour/minute hands) for the "Recently Used" pseudo-category - the
        // same hand-drawn line-art treatment as "Pinned"'s star, for the same reason: it's not a
        // real tool category with its own emoji to borrow.
        private static void DrawClock(Graphics g, Pen pen, Rectangle b)
        {
            var cx = b.Left + b.Width / 2f;
            var cy = b.Top + b.Height / 2f;
            var r = Math.Min(b.Width, b.Height) / 2f - pen.Width;

            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            g.DrawLine(pen, cx, cy, cx, cy - r * 0.6f); // minute hand, pointing up
            g.DrawLine(pen, cx, cy, cx + r * 0.45f, cy + r * 0.1f); // hour hand, pointing right-ish
        }
    }
}
