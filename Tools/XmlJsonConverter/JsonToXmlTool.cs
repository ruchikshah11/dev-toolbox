using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.XmlJsonConverter
{
    public class JsonToXmlTool : ITool
    {
        public string Category => "Converters";
        public string Name => "JSON to XML Converter";
        public string Description => "Converts JSON to an XML document.";

        public Control CreateView() => new TextTransformControl(
            "Paste some JSON",
            "XML Result",
            new[]
            {
                new TextTransformAction("Convert to XML", XmlJsonConversionService.JsonToXml, Primary: true)
            },
            contentKind: TextTransformContentKind.Json,
            outputContentKind: TextTransformContentKind.Markup);
    }
}
