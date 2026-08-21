using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.CsvEscape
{
    public class CsvEscapeTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "CSV Escape";
        public string Description => "Escapes or unescapes a single RFC4180 CSV field value.";

        public Control CreateView() => new TextTransformControl(
            "Enter the field value to CSV-escape, or a CSV field to unescape",
            "Result",
            new[]
            {
                new TextTransformAction("Escape", CsvEscapeService.Escape, Primary: true),
                new TextTransformAction("Unescape", CsvEscapeService.Unescape)
            });
    }
}
