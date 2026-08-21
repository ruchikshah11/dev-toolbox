using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.XmlEscape
{
    public class XmlEscapeTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "XML Escape";
        public string Description => "Escapes or unescapes the five XML predefined entities plus numeric character references, or wraps/extracts a <![CDATA[ ... ]]> section.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to XML-escape/CDATA-wrap, or escaped/CDATA-wrapped text to reverse",
            "Result",
            new[]
            {
                new TextTransformAction("Escape", XmlEscapeService.Escape, Primary: true),
                new TextTransformAction("Unescape", XmlEscapeService.Unescape),
                new TextTransformAction("Wrap in CDATA", XmlEscapeService.WrapInCData),
                new TextTransformAction("Extract from CDATA", XmlEscapeService.ExtractFromCData)
            });
    }
}
