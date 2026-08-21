using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.JsMinifier
{
    public class JsBeautifierTool : ITool
    {
        public string Category => "Code Minifiers / Beautifier";
        public string Name => "JavaScript Beautifier";
        public string Description => "Pretty-prints minified or condensed JavaScript into readable, indented code.";

        public Control CreateView() => new TextTransformControl(
            "Enter JavaScript to beautify",
            "Result",
            new[]
            {
                new TextTransformAction("Beautify", JsMinifierService.Beautify, Primary: true)
            });
    }
}
