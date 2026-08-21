namespace DevToolbox.Tools.HtmlViewer
{
    /// <summary>
    /// A RichTextBox that raises ViewChanged whenever its visible line range might have shifted
    /// (scrolling, mouse wheel, or an arrow-key/caret move) so a sibling line-number gutter can
    /// repaint in sync - RichTextBox has no built-in scroll event, hence intercepting the
    /// relevant window messages instead.
    /// </summary>
    internal class CodeEditorBox : RichTextBox
    {
        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int EM_LINESCROLL = 0x00B6;
        private const int WM_KEYUP = 0x0101;

        public event EventHandler? ViewChanged;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg is WM_VSCROLL or WM_MOUSEWHEEL or EM_LINESCROLL or WM_KEYUP)
            {
                ViewChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
