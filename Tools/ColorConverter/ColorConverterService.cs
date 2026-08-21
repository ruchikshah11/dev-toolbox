using System.Globalization;
using System.Text.RegularExpressions;

namespace DevToolbox.Tools.ColorConverter
{
    public readonly record struct ColorConversionResult(int R, int G, int B, string Hex, string Rgb, string Hsl);

    public static class ColorConverterService
    {
        public static ColorConversionResult Parse(string input)
        {
            input = (input ?? string.Empty).Trim();
            if (input.Length == 0) throw new FormatException("Enter a color as hex (#RRGGBB), rgb(r, g, b), or hsl(h, s%, l%).");

            var rgb = TryParseHex(input) ?? TryParseRgb(input) ?? TryParseHsl(input)
                ?? throw new FormatException("Unrecognized color format. Use #RRGGBB, rgb(r, g, b), or hsl(h, s%, l%).");

            return Build(rgb.r, rgb.g, rgb.b);
        }

        private static ColorConversionResult Build(int r, int g, int b)
        {
            var hex = $"#{r:X2}{g:X2}{b:X2}";
            var rgbText = $"rgb({r}, {g}, {b})";
            var (h, s, l) = RgbToHsl(r, g, b);
            var hslText = $"hsl({h:0}, {s:0}%, {l:0}%)";
            return new ColorConversionResult(r, g, b, hex, rgbText, hslText);
        }

        private static (int r, int g, int b)? TryParseHex(string input)
        {
            var s = input.Length > 0 && input[0] == '#' ? input.Substring(1) : input;
            if (s.Length == 3 && s.All(Uri.IsHexDigit))
            {
                return (
                    Convert.ToInt32(new string(s[0], 2), 16),
                    Convert.ToInt32(new string(s[1], 2), 16),
                    Convert.ToInt32(new string(s[2], 2), 16));
            }
            if (s.Length == 6 && s.All(Uri.IsHexDigit))
            {
                return (
                    Convert.ToInt32(s.Substring(0, 2), 16),
                    Convert.ToInt32(s.Substring(2, 2), 16),
                    Convert.ToInt32(s.Substring(4, 2), 16));
            }
            return null;
        }

        private static (int r, int g, int b)? TryParseRgb(string input)
        {
            var match = Regex.Match(input, @"^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*(?:,\s*[\d.]+\s*)?\)$",
                RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            return (
                Clamp(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)),
                Clamp(int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)),
                Clamp(int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)));
        }

        private static (int r, int g, int b)? TryParseHsl(string input)
        {
            var match = Regex.Match(input, @"^hsla?\(\s*(-?[\d.]+)\s*,\s*([\d.]+)%\s*,\s*([\d.]+)%\s*(?:,\s*[\d.]+\s*)?\)$",
                RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var h = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var s = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) / 100.0;
            var l = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) / 100.0;
            return HslToRgb(h, s, l);
        }

        private static int Clamp(int v) => Math.Max(0, Math.Min(255, v));

        private static (double h, double s, double l) RgbToHsl(int r255, int g255, int b255)
        {
            double r = r255 / 255.0, g = g255 / 255.0, b = b255 / 255.0;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var l = (max + min) / 2.0;

            double h = 0, s = 0;
            if (max != min)
            {
                var d = max - min;
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;
                h *= 60;
            }

            return (h, s * 100, l * 100);
        }

        private static (int r, int g, int b) HslToRgb(double h, double s, double l)
        {
            double r, g, b;
            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                var p = 2 * l - q;
                var hk = ((h % 360) + 360) % 360 / 360.0;
                r = HueToRgb(p, q, hk + 1.0 / 3);
                g = HueToRgb(p, q, hk);
                b = HueToRgb(p, q, hk - 1.0 / 3);
            }

            return ((int)Math.Round(r * 255), (int)Math.Round(g * 255), (int)Math.Round(b * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }
    }
}
