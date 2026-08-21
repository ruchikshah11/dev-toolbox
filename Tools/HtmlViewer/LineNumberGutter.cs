using DevToolbox.UI;

namespace DevToolbox.Tools.HtmlViewer
{
    /// <summary>
    /// A narrow, custom-painted strip to the left of a CodeEditorBox showing 1-based line
    /// numbers aligned to whichever lines are currently scrolled into view.
    /// </summary>
    internal class LineNumberGutter : Control
    {
        private readonly CodeEditorBox _editor;

        public LineNumberGutter(CodeEditorBox editor)
        {
            _editor = editor;
            Width = 44;
            DoubleBuffered = true;
            _editor.ViewChanged += (_, _) => Invalidate();
            _editor.TextChanged += (_, _) => Invalidate();
            _editor.Resize += (_, _) => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.Card);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);

            if (_editor.ClientSize.Height <= 0 || !_editor.IsHandleCreated) return;

            var firstCharIndex = _editor.GetCharIndexFromPosition(new Point(1, 1));
            var firstLine = _editor.GetLineFromCharIndex(firstCharIndex);
            var totalLines = Math.Max(1, _editor.Lines.Length);

            var font = _editor.Font;
            using var brush = new SolidBrush(Theme.TextMuted);

            for (var line = firstLine; line < totalLines; line++)
            {
                var charIndex = _editor.GetFirstCharIndexFromLine(line);
                if (charIndex < 0) break;

                var y = _editor.GetPositionFromCharIndex(charIndex).Y;
                if (y > Height) break;

                var text = (line + 1).ToString();
                var size = e.Graphics.MeasureString(text, font);
                e.Graphics.DrawString(text, font, brush, Width - 8 - size.Width, y);
            }
        }
    }
}
