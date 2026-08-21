using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.CsvConverter
{
    public class CsvToJsonTool : ITool
    {
        public string Category => "Converters";
        public string Name => "CSV to JSON Converter";
        public string Description => "Converts CSV data (with a header row) to JSON.";

        public Control CreateView() => new TextTransformControl(
            "Paste CSV data (first row = column headers)",
            "JSON Result",
            new[]
            {
                new TextTransformAction("Convert to JSON", CsvConversionService.CsvToJson, Primary: true)
            },
            outputContentKind: TextTransformContentKind.Json);
    }
}
