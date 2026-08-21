using DevToolbox.Core;

namespace DevToolbox.Tools.MarkdownPreviewer
{
    public class MarkdownPreviewerTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "Markdown Previewer";
        public string Description => "A live, side-by-side Markdown editor and preview - renders as you type, CommonMark-compliant.";

        /// <summary>Creates a fresh Markdown Previewer view instance for the shell's content area.</summary>
        public Control CreateView() => new MarkdownPreviewerControl();
    }
}
