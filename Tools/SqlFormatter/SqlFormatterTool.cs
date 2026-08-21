using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.SqlFormatter
{
    public class SqlFormatterTool : ITool
    {
        public string Category => "Formatters";
        public string Name => "SQL Formatter";
        public string Description => "Beautifies SQL by uppercasing keywords and reflowing clauses onto their own lines.";

        public Control CreateView() => new TextTransformControl(
            "Paste your SQL statement here",
            "Formatted SQL",
            new[]
            {
                new TextTransformAction("Format SQL", SqlFormatterService.Format, Primary: true)
            });
    }
}
