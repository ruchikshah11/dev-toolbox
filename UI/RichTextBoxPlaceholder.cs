namespace DevToolbox.UI
{
    /// <summary>
    /// Adds TextBox-style watermark/hint text to a RichTextBox (which has no native
    /// PlaceholderText property) - shown in a muted color while the box is empty and unfocused,
    /// cleared the moment the user clicks in or types. Used on "Choose File..." input panes where
    /// an otherwise-empty box next to a file button reads as "file upload only" - the hint makes
    /// clear that pasting/typing directly works too.
    /// </summary>
    internal sealed class RichTextBoxPlaceholder
    {
        private readonly RichTextBox _rtb;
        private readonly string _placeholder;
        private bool _showingPlaceholder;
        private bool _internalChange;

        public RichTextBoxPlaceholder(RichTextBox rtb, string placeholder)
        {
            _rtb = rtb;
            _placeholder = placeholder;

            _rtb.GotFocus += (_, _) =>
            {
                if (_showingPlaceholder) SetText(string.Empty, Theme.Text);
            };
            _rtb.LostFocus += (_, _) =>
            {
                if (_rtb.TextLength == 0) SetText(_placeholder, Theme.TextMuted);
            };
            // Any text change this class didn't itself make (typing, a "Choose File" load, ...)
            // is real content, even if it happens to arrive while the box never lost focus.
            _rtb.TextChanged += (_, _) =>
            {
                if (!_internalChange) _showingPlaceholder = false;
            };

            SetText(_placeholder, Theme.TextMuted);
        }

        /// <summary>The real text the user entered, or "" while the placeholder is showing.</summary>
        public string GetText() => _showingPlaceholder ? string.Empty : _rtb.Text;

        private void SetText(string text, Color color)
        {
            _internalChange = true;
            _rtb.Text = text;
            _internalChange = false;
            _showingPlaceholder = text == _placeholder;

            // Runs after the assignment above (and everything it triggered, including any
            // syntax highlighter also wired to TextChanged) so this color always wins.
            _rtb.SelectAll();
            _rtb.SelectionColor = color;
            _rtb.Select(0, 0);
        }
    }
}
