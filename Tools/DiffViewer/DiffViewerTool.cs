using DevToolbox.Core;

namespace DevToolbox.Tools.DiffViewer
{
    public class DiffViewerTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "Text/JSON/XML Diff Viewer";
        public string Description => "Compares two blocks of text, JSON, or XML (pretty-printed first) line by line and highlights additions/removals.";

        public Control CreateView() => new DiffViewerControl();
    }
}
