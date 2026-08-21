using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.JavaScriptEscape
{
    public class JavaScriptEscapeTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "JavaScript Escape";
        public string Description => "Escapes or unescapes text for use inside a JavaScript string literal body.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to escape, or an escaped literal body to unescape",
            "Result",
            new[]
            {
                new TextTransformAction("Escape", JavaScriptEscapeService.Escape, Primary: true),
                new TextTransformAction("Unescape", JavaScriptEscapeService.Unescape)
            });
    }
}
