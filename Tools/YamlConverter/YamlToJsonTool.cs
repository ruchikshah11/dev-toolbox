using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.YamlConverter
{
    public class YamlToJsonTool : ITool
    {
        public string Category => "Converters";
        public string Name => "YAML to JSON Converter";
        public string Description => "Converts YAML to JSON.";

        public Control CreateView() => new TextTransformControl(
            "Paste some YAML",
            "JSON Result",
            new[]
            {
                new TextTransformAction("Convert to JSON", YamlJsonConversionService.YamlToJson, Primary: true)
            },
            outputContentKind: TextTransformContentKind.Json);
    }
}
