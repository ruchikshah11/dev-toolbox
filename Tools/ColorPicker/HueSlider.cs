using System.Drawing.Drawing2D;

namespace DevToolbox.Tools.ColorPicker
{
    /// <summary>
    /// Horizontal rainbow slider for picking a hue (0-360 degrees). Raises
    /// <see cref="HueChanged"/> whenever the user clicks or drags across it.
    /// </summary>
    internal class HueSlider : Panel
    {
        public double Hue { get; private set; }

        public event EventHandler? HueChanged;

        /// <summary>Creates the slider and turns on double buffering so dragging the handle doesn't flicker.</summary>
        public HueSlider()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            MouseDown += (_, e) => PickAt(e.X);
            MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) PickAt(e.X); };
        }

        /// <summary>Sets the hue directly (e.g. when a color is sampled from an image) and repaints the handle position, without raising <see cref="HueChanged"/> - the caller already knows the new hue and is updating everything itself.</summary>
        public void SetHue(double hue)
        {
            Hue = ((hue % 360) + 360) % 360;
            Invalidate();
        }

        /// <summary>Converts a mouse X position into a hue value (clamped to the slider bounds), repaints the handle, and raises <see cref="HueChanged"/>.</summary>
        private void PickAt(int x)
        {
            var clamped = x < 0 ? 0 : x > Width - 1 ? Width - 1 : x;
            Hue = Width <= 1 ? 0 : clamped / (double)(Width - 1) * 360;
            Invalidate();
            HueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Paints the full rainbow gradient (red-yellow-green-cyan-blue-magenta-red) and the current hue's handle position.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var rect = new Rectangle(0, 0, Width, Height);
            var rainbow = new[]
            {
                Color.FromArgb(255, 0, 0), Color.FromArgb(255, 255, 0), Color.FromArgb(0, 255, 0),
                Color.FromArgb(0, 255, 255), Color.FromArgb(0, 0, 255), Color.FromArgb(255, 0, 255),
                Color.FromArgb(255, 0, 0)
            };

            using (var brush = new LinearGradientBrush(rect, rainbow[0], rainbow[rainbow.Length - 1], 0f))
            {
                brush.InterpolationColors = new ColorBlend(rainbow.Length)
                {
                    Colors = rainbow,
                    Positions = Enumerable.Range(0, rainbow.Length).Select(i => i / (float)(rainbow.Length - 1)).ToArray()
                };
                e.Graphics.FillRectangle(brush, rect);
            }

            var handleX = (int)(Hue / 360 * (Width - 1));
            using var whitePen = new Pen(Color.White, 2);
            using var darkPen = new Pen(Color.Black, 1);
            e.Graphics.DrawRectangle(whitePen, handleX - 3, 0, 6, Height - 1);
            e.Graphics.DrawRectangle(darkPen, handleX - 4, -1, 8, Height + 1);
        }
    }
}
