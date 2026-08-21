using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.XsdGenerator
{
    public class XsdGeneratorTool : ITool
    {
        public string Category => "Converters";
        public string Name => "XSD Generator";
        public string Description => "Infers an XML Schema (XSD) from a sample XML document.";

        public Control CreateView() => new TextTransformControl(
            "Paste a sample XML document",
            "Inferred XSD",
            new[]
            {
                new TextTransformAction("Generate XSD", XsdGeneratorService.GenerateXsd, Primary: true)
            },
            contentKind: TextTransformContentKind.Markup,
            outputContentKind: TextTransformContentKind.Markup);
    }
}
