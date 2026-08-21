using DevToolbox.Core;

namespace DevToolbox.Tools.HtmlFormatter
{
    public class HtmlFormatterTool : ITool
    {
        public string Category => "Formatters";
        public string Name => "HTML Formatter";
        public string Description => "Pretty-prints and re-indents HTML markup, or collapses it to a single compact line.";

        public Control CreateView() => new HtmlFormatterControl();
    }
}
