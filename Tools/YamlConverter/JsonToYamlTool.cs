using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.YamlConverter
{
    public class JsonToYamlTool : ITool
    {
        public string Category => "Converters";
        public string Name => "JSON to YAML Converter";
        public string Description => "Converts JSON to YAML.";

        public Control CreateView() => new TextTransformControl(
            "Paste some JSON",
            "YAML Result",
            new[]
            {
                new TextTransformAction("Convert to YAML", YamlJsonConversionService.JsonToYaml, Primary: true)
            },
            contentKind: TextTransformContentKind.Json);
    }
}
