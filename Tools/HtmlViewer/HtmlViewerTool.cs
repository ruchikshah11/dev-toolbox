using DevToolbox.Core;

namespace DevToolbox.Tools.HtmlViewer
{
    public class HtmlViewerTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "HTML Viewer";
        public string Description => "A live, side-by-side HTML editor and preview - syntax-highlighted with line numbers on the left, rendered output on the right, updating as you type.";

        /// <summary>Creates a fresh HTML Viewer view instance for the shell's content area.</summary>
        public Control CreateView() => new HtmlViewerControl();
    }
}
