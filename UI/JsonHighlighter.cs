using DevToolbox.Tools.JsonFormatter;

namespace DevToolbox.UI
{
    /// <summary>
    /// Colorizes a RichTextBox's current text as JSON/JSONC, preserving the caret and scroll
    /// position across the rebuild - the JSON counterpart to MarkupHighlighter, sharing the same
    /// flicker-free WM_SETREDRAW technique. Used by both JsonFormatterControl's input pane and
    /// any TextTransformControl instance opted into JSON coloring (e.g. the JSON Validator).
    /// </summary>
    internal static class JsonHighlighter
    {
        public static void Highlight(RichTextBox rtb)
        {
            var selectionStart = rtb.SelectionStart;
            var selectionLength = rtb.SelectionLength;
            var scrollPos = NativeMethods.GetScrollPos(rtb);

            NativeMethods.SuspendDrawing(rtb);
            try
            {
                var segments = JsonInputColorizer.BuildSegments(rtb.Text);
                rtb.SelectAll();
                rtb.SelectionColor = JsonColors.Structural;

                var pos = 0;
                foreach (var segment in segments)
                {
                    if (segment.Text.Length > 0)
                    {
                        rtb.Select(pos, segment.Text.Length);
                        rtb.SelectionColor = JsonColors.For(segment.Kind);
                    }
                    pos += segment.Text.Length;
                }

                rtb.Select(selectionStart, selectionLength);
                NativeMethods.SetScrollPos(rtb, scrollPos);
            }
            finally
            {
                NativeMethods.ResumeDrawing(rtb);
            }
        }
    }
}
