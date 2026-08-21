using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.CssMinifier
{
    public class CssBeautifierTool : ITool
    {
        public string Category => "Code Minifiers / Beautifier";
        public string Name => "CSS Beautifier";
        public string Description => "Pretty-prints minified or condensed CSS into readable, indented stylesheet.";

        public Control CreateView() => new TextTransformControl(
            "Enter CSS to beautify",
            "Result",
            new[]
            {
                new TextTransformAction("Beautify", CssMinifierService.Beautify, Primary: true)
            });
    }
}
