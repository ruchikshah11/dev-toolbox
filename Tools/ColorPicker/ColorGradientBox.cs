using System.Drawing.Drawing2D;

namespace DevToolbox.Tools.ColorPicker
{
    /// <summary>
    /// Saturation/Value picker square for a fixed hue: the X axis is saturation (0 at the left
    /// edge, 1 at the right edge) and the Y axis is value/brightness (1 at the top, 0 at the
    /// bottom). Raises <see cref="SelectionChanged"/> whenever the user clicks or drags inside it.
    /// </summary>
    internal class ColorGradientBox : Panel
    {
        public double Hue { get; private set; }
        public double Saturation { get; private set; } = 1.0;
        public double Value { get; private set; } = 1.0;

        public event EventHandler? SelectionChanged;

        /// <summary>Creates the box and turns on double buffering so dragging the marker doesn't flicker.</summary>
        public ColorGradientBox()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            MouseDown += (_, e) => PickAt(e.Location);
            MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) PickAt(e.Location); };
        }

        /// <summary>Changes which hue this box renders the gradient for, without touching the current saturation/value selection.</summary>
        public void SetHue(double hue)
        {
            Hue = hue;
            Invalidate();
        }

        /// <summary>Sets hue, saturation, and value all at once - used when a color arrives from outside a drag (e.g. sampled from an image), where all three can change together.</summary>
        public void SetSelection(double hue, double saturation, double value)
        {
            Hue = hue;
            Saturation = ClampUnit(saturation);
            Value = ClampUnit(value);
            Invalidate();
        }

        /// <summary>Clamps a 0-1 fraction into range - a stand-in for Math.Clamp, which isn't available on .NET Framework 4.7.2.</summary>
        private static double ClampUnit(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

        /// <summary>Converts a mouse position into saturation/value (clamped to the box bounds), repaints the marker, and raises <see cref="SelectionChanged"/>.</summary>
        private void PickAt(Point location)
        {
            var x = Clamp(location.X, 0, Width - 1);
            var y = Clamp(location.Y, 0, Height - 1);
            Saturation = Width <= 1 ? 0 : x / (double)(Width - 1);
            Value = Height <= 1 ? 0 : 1 - y / (double)(Height - 1);
            Invalidate();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Clamps an integer into [min, max] - a stand-in for Math.Clamp, which isn't available on .NET Framework 4.7.2.</summary>
        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        /// <summary>Paints the saturation (white-to-hue, left-to-right) over value (transparent-to-black, top-to-bottom) gradient, plus the current selection marker.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var (hr, hg, hb) = ColorPickerService.HsvToRgb(Hue, 1, 1);
            var hueColor = Color.FromArgb(hr, hg, hb);
            var rect = new Rectangle(0, 0, Width, Height);

            using (var saturationBrush = new LinearGradientBrush(rect, Color.White, hueColor, 0f))
            {
                e.Graphics.FillRectangle(saturationBrush, rect);
            }
            // Alpha-blending transparent-to-black over the saturation gradient darkens it towards
            // the bottom without needing a second full color computation per pixel.
            using (var valueBrush = new LinearGradientBrush(rect, Color.Transparent, Color.Black, 90f))
            {
                e.Graphics.FillRectangle(valueBrush, rect);
            }

            var markerX = (int)(Saturation * (Width - 1));
            var markerY = (int)((1 - Value) * (Height - 1));
            using var whitePen = new Pen(Color.White, 2);
            using var darkPen = new Pen(Color.Black, 1);
            e.Graphics.DrawEllipse(whitePen, markerX - 6, markerY - 6, 12, 12);
            e.Graphics.DrawEllipse(darkPen, markerX - 7, markerY - 7, 14, 14);
        }
    }
}
