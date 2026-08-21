using DevToolbox.Core;

namespace DevToolbox.Tools.XmlFormatter
{
    public class XmlFormatterTool : ITool
    {
        public string Category => "Formatters";
        public string Name => "XML Formatter";
        public string Description => "Pretty-prints and re-indents XML, or collapses it to a single compact line.";

        public Control CreateView() => new XmlFormatterControl();
    }
}
