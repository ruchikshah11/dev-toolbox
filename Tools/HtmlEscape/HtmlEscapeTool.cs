using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.HtmlEscape
{
    public class HtmlEscapeTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "HTML Escape";
        public string Description => "HTML-encodes or decodes text so it is safe to embed inside HTML markup.";

        public Control CreateView() => new TextTransformControl(
            "Enter the text to HTML-escape, or HTML-escaped text to unescape",
            "Result",
            new[]
            {
                new TextTransformAction("Escape", HtmlEscapeService.Escape, Primary: true),
                new TextTransformAction("Unescape", HtmlEscapeService.Unescape)
            });
    }
}
