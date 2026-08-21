using System.Linq;

namespace DevToolbox.UI
{
    /// <summary>
    /// A self-contained replacement for showing colorized, read-only formatted text - built from
    /// scratch rather than on top of RichTextBox or WebBrowser. Both of those carry their own
    /// internal engines (RichEdit, IE11/Trident) with wrapping/scrolling behavior that couldn't be
    /// made to behave predictably for this app's formatter output panes - a RichTextBox-based
    /// rewrite was tried again later in this same investigation (to get native click-drag
    /// selection for free) but was abandoned: EM_SCROLLCARET/WM_VSCROLL didn't reliably reach the
    /// true end of a long wrapped document (confirmed directly - scrollbar position barely moved
    /// past the top after scrolling to the very last character), reproducing the exact bug this
    /// class exists to avoid.
    ///
    /// This is a ListBox, not a custom-painted Panel: each visual (already word-wrapped) line is
    /// one owner-drawn list item, so scrolling is ListBox's own native, independently-scrollbarred
    /// mechanism - deliberately a different rendering pipeline than a Panel.AutoScroll-based
    /// approach, since that one's on-screen paint could get stuck out of sync with what it had
    /// actually computed (confirmed via a DrawToBitmap-vs-on-screen comparison). The word-wrap
    /// algorithm itself (word-boundary wrapping, hard-breaking only a run with no whitespace at
    /// all) was already verified correct in isolation and carries over unchanged.
    /// </summary>
    internal class SyntaxTextPanel : ListBox
    {
        private readonly record struct Run(string Text, Color Color);

        private List<Run> _segments = new();

        public string PlainText { get; private set; } = "";

        public SyntaxTextPanel()
        {
            BorderStyle = BorderStyle.None;
            BackColor = Theme.Card;
            Font = Theme.MonoFont;
            DrawMode = DrawMode.OwnerDrawFixed;
            // SelectionMode.None (Win32 LBS_NOSEL) turned out to also disable ListBox's default
            // mouse-wheel scroll handling - confirmed directly (200 wheel ticks produced no
            // movement at all). SelectionMode.One keeps that native behavior; OnDrawItem below
            // ignores the Selected state so it never actually shows a selection highlight.
            SelectionMode = SelectionMode.One;
            IntegralHeight = false;
            ItemHeight = Math.Max(1, Font.Height);

            // No native click-drag text selection here (that's what the RichTextBox rewrite was
            // trying to add, and why it was abandoned - see the class remarks) - Ctrl+A/Ctrl+C are
            // wired directly to "copy everything" instead, which is what a validator/formatter
            // output actually needs most of the time and doesn't require a hand-built selection
            // renderer on top of an owner-drawn ListBox.
            KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode is Keys.A or Keys.C && PlainText.Length > 0)
                {
                    Clipboard.SetText(PlainText);
                    e.Handled = true;
                }
            };
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ItemHeight = Math.Max(1, Font.Height);
            Rewrap();
        }

        private const int WM_MOUSEWHEEL = 0x020A;

        /// <summary>
        /// Replaces the native ListBox mouse-wheel handling entirely. Confirmed directly (via
        /// LB_GETTOPINDEX after simulated wheel input) that the native handler stops short of the
        /// true end: for a 184-item list showing 55 rows, it plateaued at TopIndex=120 instead of
        /// the correct max of 129, permanently hiding the last 9 lines - matching the "can't scroll
        /// to the very end" symptom exactly. Computing and setting TopIndex ourselves, and eating
        /// the message before it reaches the native window proc, avoids that native shortfall.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL)
            {
                var delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                var linesPerNotch = SystemInformation.MouseWheelScrollLines <= 0 ? 3 : SystemInformation.MouseWheelScrollLines;
                var visibleRows = Math.Max(1, ClientSize.Height / Math.Max(1, ItemHeight));
                var maxTop = Math.Max(0, Items.Count - visibleRows);
                var newTop = TopIndex - delta / 120 * linesPerNotch;
                TopIndex = Math.Max(0, Math.Min(maxTop, newTop));
                return;
            }

            base.WndProc(ref m);
        }

        public void Clear()
        {
            _segments = new List<Run>();
            PlainText = "";
            Items.Clear();
        }

        public void SetContent(IEnumerable<(string Text, Color Color)> segments)
        {
            _segments = segments.Where(s => s.Text.Length > 0).Select(s => new Run(s.Text, s.Color)).ToList();
            PlainText = string.Concat(_segments.Select(s => s.Text));
            Rewrap();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Rewrap();
        }

        /// <summary>
        /// Rebuilds the list's items for the current width. Wraps at whitespace boundaries like
        /// ordinary word-wrap, but a single "word" longer than the available width is hard-broken
        /// character by character instead of being left to overflow - the one case that plain
        /// word-wrap (RichTextBox's WordWrap, or a browser's word-wrap:break-word) wasn't reliably
        /// handling for this app's content (HTML-encoded attribute values with no spaces at all).
        /// </summary>
        private void Rewrap()
        {
            // A vertical scrollbar (native to ListBox, shown automatically once needed) eats into
            // the usable width - accounted for up front rather than after the fact, so wrapping
            // doesn't shift once enough lines appear to trigger the scrollbar.
            var usableWidth = ClientSize.Width - 16 - SystemInformation.VerticalScrollBarWidth;
            var charWidth = Math.Max(1, TextRenderer.MeasureText("MMMMMMMMMM", Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width / 10);
            var maxChars = Math.Max(4, usableWidth / charWidth);

            var lines = new List<List<Run>>();

            // Accumulates one source line's worth of runs (i.e. up to the next '\n', which may
            // span several differently-colored segments) before wrapping it as a whole - see
            // WrapLogicalLine for why this can't be done per-segment.
            var logicalLine = new List<Run>();

            void FlushLogicalLine()
            {
                WrapLogicalLine(logicalLine, maxChars, lines);
                logicalLine = new List<Run>();
            }

            foreach (var seg in _segments)
            {
                var start = 0;
                while (start <= seg.Text.Length)
                {
                    var newline = seg.Text.IndexOf('\n', start);
                    var end = newline < 0 ? seg.Text.Length : newline;
                    if (end > start) logicalLine.Add(new Run(seg.Text.Substring(start, end - start), seg.Color));

                    if (newline < 0) break;
                    FlushLogicalLine();
                    start = newline + 1;
                }
            }

            FlushLogicalLine();

            BeginUpdate();
            Items.Clear();
            foreach (var line in lines) Items.Add(new LineItem(line));
            EndUpdate();
        }

        /// <summary>
        /// Wraps one source line - given as an ordered list of colored runs, no embedded newlines -
        /// into one or more visual lines of at most maxChars characters each, breaking at the last
        /// whitespace found across the WHOLE line's text. Searching per-run instead (as an earlier
        /// version did) missed whitespace that fell in a different-colored run than the word after
        /// it - e.g. an attribute name like "Description" gets its own syntax-highlight color
        /// separate from the space before it, so when that run happened to start right where a
        /// visual line was already full, there was no whitespace to find within "Description"
        /// itself and it got hard-broken mid-word instead of wrapping at the space before it.
        /// </summary>
        private static void WrapLogicalLine(List<Run> runs, int maxChars, List<List<Run>> outLines)
        {
            var fullText = string.Concat(runs.Select(r => r.Text));
            if (fullText.Length == 0)
            {
                outLines.Add(new List<Run>());
                return;
            }

            var pos = 0;
            while (pos < fullText.Length)
            {
                var chunkEnd = Math.Min(fullText.Length, pos + maxChars);
                var breakAt = chunkEnd;

                if (chunkEnd < fullText.Length)
                {
                    for (var i = chunkEnd - 1; i >= pos; i--)
                    {
                        if (char.IsWhiteSpace(fullText[i])) { breakAt = i + 1; break; }
                    }
                }

                outLines.Add(SliceRuns(runs, pos, breakAt));
                pos = breakAt;
            }
        }

        /// <summary>Extracts the colored sub-runs covering [start, end) of the runs' concatenated text.</summary>
        private static List<Run> SliceRuns(List<Run> runs, int start, int end)
        {
            var result = new List<Run>();
            var offset = 0;
            foreach (var run in runs)
            {
                var runStart = offset;
                var runEnd = offset + run.Text.Length;
                offset = runEnd;

                var sliceStart = Math.Max(start, runStart);
                var sliceEnd = Math.Min(end, runEnd);
                if (sliceStart < sliceEnd)
                {
                    result.Add(new Run(run.Text.Substring(sliceStart - runStart, sliceEnd - sliceStart), run.Color));
                }
                if (runEnd >= end) break;
            }
            return result;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // Always paints as plain/unselected, regardless of e.State - this is a read-only
            // display, not an interactive list, so a selection highlight (needed to keep
            // SelectionMode.One's native mouse-wheel scrolling, see the constructor) should never
            // actually be visible.
            using (var bg = new SolidBrush(BackColor)) e.Graphics.FillRectangle(bg, e.Bounds);

            if (e.Index >= 0 && e.Index < Items.Count && Items[e.Index] is LineItem item)
            {
                var g = e.Graphics;
                var x = e.Bounds.Left + 8;
                foreach (var run in item.Runs)
                {
                    // NoPrefix matters here specifically: without it, TextRenderer treats '&' as a
                    // mnemonic-underline marker (like "&File") and silently drops it instead of
                    // drawing it - this content is full of '&' from HTML-encoded output (&quot;,
                    // &amp;, &#39;, ...), so every one of those was quietly losing its '&'.
                    const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.NoPrefix;
                    TextRenderer.DrawText(g, run.Text, Font, new Point(x, e.Bounds.Top), run.Color, BackColor, flags);
                    x += TextRenderer.MeasureText(g, run.Text, Font, Size.Empty, flags).Width;
                }
            }
        }

        private sealed class LineItem
        {
            public LineItem(List<Run> runs)
            {
                Runs = runs;
                Text = string.Concat(runs.Select(r => r.Text));
            }

            public List<Run> Runs { get; }
            private string Text { get; }
            public override string ToString() => Text;
        }
    }
}
