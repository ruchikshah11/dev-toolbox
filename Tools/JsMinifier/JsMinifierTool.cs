using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.JsMinifier
{
    public class JsMinifierTool : ITool
    {
        public string Category => "Code Minifiers / Beautifier";
        public string Name => "JavaScript Minifier";
        public string Description => "Condenses JavaScript by stripping whitespace/comments and shortening names.";

        public Control CreateView() => new TextTransformControl(
            "Enter JavaScript to minify",
            "Result",
            new[]
            {
                new TextTransformAction("Minify", JsMinifierService.Minify, Primary: true)
            });
    }
}
