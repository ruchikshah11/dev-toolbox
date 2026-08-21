using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.XmlJsonConverter
{
    public class XmlToJsonTool : ITool
    {
        public string Category => "Converters";
        public string Name => "XML to JSON Converter";
        public string Description => "Converts an XML document to JSON.";

        public Control CreateView() => new TextTransformControl(
            "Paste an XML document",
            "JSON Result",
            new[]
            {
                new TextTransformAction("Convert to JSON", XmlJsonConversionService.XmlToJson, Primary: true)
            },
            contentKind: TextTransformContentKind.Markup,
            outputContentKind: TextTransformContentKind.Json);
    }
}
