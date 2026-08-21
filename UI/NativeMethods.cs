using System.Runtime.InteropServices;

namespace DevToolbox.UI
{
    /// <summary>
    /// Win32 interop needed to recolor a RichTextBox on every keystroke without visible flicker
    /// or the caret/view jumping: WM_SETREDRAW suspends painting during the recolor pass, and
    /// EM_GETSCROLLPOS/EM_SETSCROLLPOS save and restore the scroll position around it (calling
    /// RichTextBox.Select() to apply per-segment colors otherwise auto-scrolls to reveal each
    /// selection in turn). Shared by every live-highlighted editor (HTML Viewer, JSON Formatter's
    /// input pane, ...) rather than each one reimplementing the same interop.
    /// </summary>
    internal static class NativeMethods
    {
        private const int WM_SETREDRAW = 0x000B;
        private const int EM_GETSCROLLPOS = 0x0400 + 221;
        private const int EM_SETSCROLLPOS = 0x0400 + 222;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref Point lParam);

        public static void SuspendDrawing(Control control) =>
            SendMessage(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

        /// <summary>
        /// Re-enables painting after SuspendDrawing and forces one immediate repaint.
        /// <paramref name="recursive"/> controls whether that repaint also covers every child
        /// control, not just the control's own surface - plain RichTextBox callers (no nested
        /// children) don't need it, but MainForm's nav panel does: a plain, non-recursive
        /// Invalidate() here was tried there first and left child Label/Panel controls (category
        /// headers, tool rows) never told to repaint after a WM_SETREDRAW suspend/resume around a
        /// Controls.Clear()+Add() churn - they'd stay blank or stale until some *unrelated* later
        /// event happened to repaint them.
        /// </summary>
        public static void ResumeDrawing(Control control, bool recursive = false)
        {
            SendMessage(control.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);

            // Invalidate() alone only *schedules* a repaint for whenever the message loop next
            // gets to it - confirmed by a direct diagnostic dump: DrawToBitmap (which forces an
            // immediate real paint) showed the control's actual state was correctly laid out and
            // wrapped all along, while the on-screen window kept showing a stale, unwrapped
            // render. Update() forces that repaint to happen synchronously, right now, instead of
            // leaving the on-screen surface stale until something else happens to repaint it.
            control.Invalidate(recursive);
            control.Update();
        }

        public static Point GetScrollPos(RichTextBox editor)
        {
            var point = new Point();
            SendMessage(editor.Handle, EM_GETSCROLLPOS, IntPtr.Zero, ref point);
            return point;
        }

        // A prior version of this retried (first via Application.DoEvents(), then via
        // BeginInvoke) because a single call can occasionally not "stick" right after a
        // WM_SETREDRAW resume. Both retry approaches caused a native AccessViolationException in
        // practice - DoEvents() reentered the message loop mid-construction, and a BeginInvoke
        // queued against a RichTextBox can still be pending when that control is disposed (e.g.
        // switching tools clears the old view's controls), so the deferred callback could end up
        // sending an EM_SETSCROLLPOS message to an already-destroyed (or since-recycled) window
        // handle. A plain, single, synchronous call - occasionally imperfect but never unsafe -
        // is the deliberate tradeoff here.
        public static void SetScrollPos(RichTextBox editor, Point point) =>
            SendMessage(editor.Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref point);
    }
}
