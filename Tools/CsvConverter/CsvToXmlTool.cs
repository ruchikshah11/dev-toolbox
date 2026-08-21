using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.CsvConverter
{
    public class CsvToXmlTool : ITool
    {
        public string Category => "Converters";
        public string Name => "CSV to XML Converter";
        public string Description => "Converts CSV data (with a header row) to XML.";

        public Control CreateView() => new TextTransformControl(
            "Paste CSV data (first row = column headers)",
            "XML Result",
            new[]
            {
                new TextTransformAction("Convert to XML", CsvConversionService.CsvToXml, Primary: true)
            },
            outputContentKind: TextTransformContentKind.Markup);
    }
}
