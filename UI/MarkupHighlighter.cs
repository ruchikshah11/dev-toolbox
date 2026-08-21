namespace DevToolbox.UI
{
    /// <summary>
    /// Colorizes a RichTextBox's current text as tag markup (HTML/XML), preserving the caret and
    /// scroll position across the rebuild via the same flicker-free WM_SETREDRAW technique as
    /// every other live-highlighted pane in the app. One method serves both cases: wire it to
    /// TextChanged for a live, as-you-type editor (the XML/HTML Formatter's input, XPath Tester's
    /// XML box, ...), or call it once right after setting a read-only output's Text (the
    /// formatted/result pane) - either way, a future palette or tokenizer tweak in
    /// MarkupSyntaxTokenizer/MarkupSyntaxColors applies everywhere at once.
    /// </summary>
    internal static class MarkupHighlighter
    {
        public static void Highlight(RichTextBox rtb)
        {
            var selectionStart = rtb.SelectionStart;
            var selectionLength = rtb.SelectionLength;
            var scrollPos = NativeMethods.GetScrollPos(rtb);

            NativeMethods.SuspendDrawing(rtb);
            try
            {
                var segments = MarkupSyntaxTokenizer.Tokenize(rtb.Text);
                rtb.SelectAll();
                rtb.SelectionColor = Theme.Text;

                var pos = 0;
                foreach (var segment in segments)
                {
                    if (segment.Text.Length > 0)
                    {
                        rtb.Select(pos, segment.Text.Length);
                        rtb.SelectionColor = MarkupSyntaxColors.For(segment.Kind);
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
