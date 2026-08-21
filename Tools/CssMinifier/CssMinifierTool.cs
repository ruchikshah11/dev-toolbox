using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.CssMinifier
{
    public class CssMinifierTool : ITool
    {
        public string Category => "Code Minifiers / Beautifier";
        public string Name => "CSS Minifier";
        public string Description => "Condenses CSS by stripping whitespace/comments and shortening values.";

        public Control CreateView() => new TextTransformControl(
            "Enter CSS to minify",
            "Result",
            new[]
            {
                new TextTransformAction("Minify", CssMinifierService.Minify, Primary: true)
            });
    }
}
