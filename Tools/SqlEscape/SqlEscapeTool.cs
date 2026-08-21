using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.SqlEscape
{
    public class SqlEscapeTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "SQL Escape";
        public string Description => "Doubles or un-doubles single quotes for safe use inside a SQL string literal.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to SQL-escape, or SQL-escaped text to unescape",
            "Result",
            new[]
            {
                new TextTransformAction("Escape", SqlEscapeService.Escape, Primary: true),
                new TextTransformAction("Unescape", SqlEscapeService.Unescape)
            });
    }
}
