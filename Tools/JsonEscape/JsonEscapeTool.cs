using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.JsonEscape
{
    public class JsonEscapeTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "JSON Escape";
        public string Description => "Produces or parses a quoted JSON string token, including the surrounding double quotes.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to JSON-escape, or a quoted JSON string token to unescape",
            "Result",
            new[]
            {
                new TextTransformAction("Escape", JsonEscapeService.Escape, Primary: true),
                new TextTransformAction("Unescape", JsonEscapeService.Unescape)
            });
    }
}
