namespace DevToolbox.Tools.ColorPicker
{
    /// <summary>
    /// Full-screen, borderless overlay used as the "pick from screen" eyedropper. Rather than a
    /// live screen-scraping loop (which would need a global mouse hook to track the cursor
    /// outside this app's own window), it takes one screenshot of the whole virtual desktop up
    /// front, displays that as its own background (so it visually looks like the live desktop),
    /// and samples pixels from that frozen bitmap as the mouse moves over it - a plain WinForms
    /// MouseMove/Click is enough since the "screen" being sampled is really just this form's
    /// own content.
    /// </summary>
    internal class ScreenColorPickerForm : Form
    {
        private readonly Bitmap _capture;

        /// <summary>The color under the cursor at the moment the user left-clicked - only meaningful when the dialog result is OK.</summary>
        public Color PickedColor { get; private set; }

        /// <summary>Takes ownership of the given full-virtual-desktop screenshot (disposed when this form closes) and positions itself to exactly cover it.</summary>
        public ScreenColorPickerForm(Bitmap capture, Point virtualScreenOrigin)
        {
            _capture = capture;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(virtualScreenOrigin, capture.Size);
            TopMost = true;
            ShowInTaskbar = false;
            KeyPreview = true;
            Cursor = Cursors.Cross;
            BackgroundImage = capture;
            BackgroundImageLayout = ImageLayout.None;
            DoubleBuffered = true;

            MouseMove += (_, _) => Invalidate();
            MouseClick += (_, e) =>
            {
                if (e.Button != MouseButtons.Left)
                {
                    DialogResult = DialogResult.Cancel;
                    return;
                }

                var px = Clamp(e.X, 0, _capture.Width - 1);
                var py = Clamp(e.Y, 0, _capture.Height - 1);
                PickedColor = _capture.GetPixel(px, py);
                DialogResult = DialogResult.OK;
            };
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel;
            };
            FormClosed += (_, _) => _capture.Dispose();
        }

        /// <summary>Draws a small swatch + hex readout that follows the cursor, showing exactly which pixel will be picked.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var cursor = PointToClient(Cursor.Position);
            var px = Clamp(cursor.X, 0, _capture.Width - 1);
            var py = Clamp(cursor.Y, 0, _capture.Height - 1);
            var color = _capture.GetPixel(px, py);

            const int boxWidth = 150;
            const int boxHeight = 40;
            var boxX = Clamp(cursor.X + 20, 0, ClientSize.Width - boxWidth);
            var boxY = Clamp(cursor.Y + 20, 0, ClientSize.Height - boxHeight);

            using var backgroundBrush = new SolidBrush(Color.FromArgb(230, 30, 30, 30));
            e.Graphics.FillRectangle(backgroundBrush, boxX, boxY, boxWidth, boxHeight);
            e.Graphics.DrawRectangle(Pens.White, boxX, boxY, boxWidth - 1, boxHeight - 1);

            using var swatchBrush = new SolidBrush(color);
            e.Graphics.FillRectangle(swatchBrush, boxX + 8, boxY + 8, 24, 24);
            e.Graphics.DrawRectangle(Pens.White, boxX + 8, boxY + 8, 24, 24);

            using var font = new Font("Segoe UI", 9f);
            e.Graphics.DrawString($"#{color.R:X2}{color.G:X2}{color.B:X2}", font, Brushes.White, boxX + 40, boxY + 12);
        }

        /// <summary>Clamps an integer into [min, max] - a stand-in for Math.Clamp, which isn't available on .NET Framework 4.7.2.</summary>
        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
