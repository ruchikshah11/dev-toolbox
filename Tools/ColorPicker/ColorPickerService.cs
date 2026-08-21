namespace DevToolbox.Tools.ColorPicker
{
    /// <summary>The color notations the Color Picker's format dropdown can display the current selection in.</summary>
    public enum ColorFormat { Hex, Rgb, Hsl, Hsv, Oklch }

    /// <summary>The color-wheel relationships the Color Picker's harmony dropdown can generate from the current hue.</summary>
    public enum ColorHarmony { None, Complementary, Analogous, Triadic, SplitComplementary, Tetradic }

    /// <summary>
    /// HSV color math for the Color Picker tool. Kept separate from ColorConverterService (which
    /// parses hex/rgb/hsl strings) since this tool works natively in HSV - hue/saturation/value -
    /// to match the gradient-square + hue-slider style of picker, not HSL.
    /// </summary>
    public static class ColorPickerService
    {
        /// <summary>Converts an HSV triple (hue in degrees 0-360, saturation/value 0-1) into RGB byte components.</summary>
        public static (byte R, byte G, byte B) HsvToRgb(double hue, double saturation, double value)
        {
            var c = value * saturation;
            var x = c * (1 - Math.Abs(hue / 60.0 % 2 - 1));
            var m = value - c;

            double r1, g1, b1;
            if (hue < 60) (r1, g1, b1) = (c, x, 0);
            else if (hue < 120) (r1, g1, b1) = (x, c, 0);
            else if (hue < 180) (r1, g1, b1) = (0, c, x);
            else if (hue < 240) (r1, g1, b1) = (0, x, c);
            else if (hue < 300) (r1, g1, b1) = (x, 0, c);
            else (r1, g1, b1) = (c, 0, x);

            return ((byte)Math.Round((r1 + m) * 255), (byte)Math.Round((g1 + m) * 255), (byte)Math.Round((b1 + m) * 255));
        }

        /// <summary>Formats RGB byte components as an uppercase "#RRGGBB" hex string.</summary>
        public static string ToHex(byte r, byte g, byte b) => $"#{r:X2}{g:X2}{b:X2}";

        /// <summary>Converts RGB byte components into an HSV triple (hue in degrees 0-360, saturation/value 0-1) - the inverse of <see cref="HsvToRgb"/>, used when a color is sampled from an image instead of dragged on the gradient box.</summary>
        public static (double H, double S, double V) RgbToHsv(byte r8, byte g8, byte b8)
        {
            var r = r8 / 255.0;
            var g = g8 / 255.0;
            var b = b8 / 255.0;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            double h;
            if (delta < 1e-9) h = 0;
            else if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * (((b - r) / delta) + 2);
            else h = 60 * (((r - g) / delta) + 4);
            if (h < 0) h += 360;

            var s = max <= 0 ? 0 : delta / max;
            return (h, s, max);
        }

        /// <summary>Converts RGB byte components into HSL (hue in degrees 0-360, saturation/lightness 0-1).</summary>
        public static (double H, double S, double L) RgbToHsl(byte r8, byte g8, byte b8)
        {
            var r = r8 / 255.0;
            var g = g8 / 255.0;
            var b = b8 / 255.0;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var l = (max + min) / 2.0;

            if (max - min < 1e-9) return (0, 0, l);

            var d = max - min;
            var s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            double h;
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;

            return (h * 60, s, l);
        }

        /// <summary>
        /// Converts RGB byte components into OKLCH (lightness 0-1, chroma, hue in degrees 0-360)
        /// using Bjorn Ottosson's OKLab formulas: sRGB -> linear RGB -> LMS -> OKLab -> polar LCh.
        /// </summary>
        public static (double L, double C, double H) RgbToOklch(byte r8, byte g8, byte b8)
        {
            var r = Linearize(r8 / 255.0);
            var g = Linearize(g8 / 255.0);
            var b = Linearize(b8 / 255.0);

            var l = Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
            var m = Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
            var s = Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

            var lightness = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
            var a = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
            var bLab = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;

            var chroma = Math.Sqrt(a * a + bLab * bLab);
            var hue = Math.Atan2(bLab, a) * 180.0 / Math.PI;
            if (hue < 0) hue += 360;

            return (lightness, chroma, hue);
        }

        /// <summary>Undoes the sRGB gamma encoding, converting a 0-1 sRGB channel value into linear light.</summary>
        private static double Linearize(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        /// <summary>Cube root that handles negative inputs - a stand-in for Math.Cbrt, which isn't available on .NET Framework 4.7.2.</summary>
        private static double Cbrt(double x) => x < 0 ? -Math.Pow(-x, 1.0 / 3.0) : Math.Pow(x, 1.0 / 3.0);

        /// <summary>
        /// Returns the hue offsets (in degrees, relative to a base hue) that make up the given
        /// harmony - e.g. Complementary is the hue directly opposite on the color wheel,
        /// Triadic is two hues 120 degrees apart from it. Each offset shares the base color's own
        /// saturation/value; only the hue changes.
        /// </summary>
        public static double[] HarmonyOffsets(ColorHarmony harmony) => harmony switch
        {
            ColorHarmony.Complementary => new double[] { 0, 180 },
            ColorHarmony.Analogous => new double[] { -30, 0, 30 },
            ColorHarmony.Triadic => new double[] { 0, 120, 240 },
            ColorHarmony.SplitComplementary => new double[] { 0, 150, 210 },
            ColorHarmony.Tetradic => new double[] { 0, 90, 180, 270 },
            _ => Array.Empty<double>()
        };

        /// <summary>Wraps a hue value back into the valid 0-360 degree range.</summary>
        public static double NormalizeHue(double hue) => ((hue % 360) + 360) % 360;

        /// <summary>Formats the color given by an HSV triple in the requested notation, ready to display and copy.</summary>
        public static string Format(ColorFormat format, double hue, double saturation, double value)
        {
            var (r, g, b) = HsvToRgb(hue, saturation, value);
            switch (format)
            {
                case ColorFormat.Rgb:
                    return $"rgb({r}, {g}, {b})";
                case ColorFormat.Hsl:
                    var (h, s, l) = RgbToHsl(r, g, b);
                    return $"hsl({Math.Round(h)}, {Math.Round(s * 100)}%, {Math.Round(l * 100)}%)";
                case ColorFormat.Hsv:
                    return $"hsv({Math.Round(hue)}, {Math.Round(saturation * 100)}%, {Math.Round(value * 100)}%)";
                case ColorFormat.Oklch:
                    var (ol, oc, oh) = RgbToOklch(r, g, b);
                    return $"oklch({ol:0.00} {oc:0.00} {Math.Round(oh)})";
                default:
                    return ToHex(r, g, b);
            }
        }
    }
}
