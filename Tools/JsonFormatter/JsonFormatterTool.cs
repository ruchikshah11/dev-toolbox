using DevToolbox.Core;

namespace DevToolbox.Tools.JsonFormatter
{
    public class JsonFormatterTool : ITool
    {
        public string Category => "Formatters";
        public string Name => "JSON Formatter";
        public string Description => "Formats a JSON string or file with a chosen indentation and bracket style, with a color-highlighted, collapsible tree view.";

        public Control CreateView() => new JsonFormatterControl();
    }
}
