namespace DevToolbox.UI
{
    // Plain Panel with double buffering turned on. Control.DoubleBuffered is only a protected
    // property, so it can't be set on a plain Panel instance from outside - this subclass exists
    // solely to expose it. Used for panels whose contents get swapped/cleared at runtime (nav
    // list, tool content area), which otherwise show a visible flash of the background color
    // between the old paint and the new one.
    internal class BufferedPanel : Panel
    {
        /// <summary>Turns on double buffering for this panel instance.</summary>
        public BufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
    }
}
