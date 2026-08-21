using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.JavaDotNetEscape
{
    public class JavaDotNetEscapeTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "Java and .Net Escape";
        public string Description => "Escapes or unescapes text for use inside a Java or C# string literal body.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to escape, or an escaped literal body to unescape",
            "Result",
            new[]
            {
                new TextTransformAction("Escape", JavaDotNetEscapeService.Escape, Primary: true),
                new TextTransformAction("Unescape", JavaDotNetEscapeService.Unescape)
            });
    }
}
