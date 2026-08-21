using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.UrlParser
{
    public class UrlParserTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "Url Parser / Query String Splitter";
        public string Description => "Breaks a URL down into its scheme, host, port, path and fragment, and splits the query string into decoded key/value pairs.";

        public Control CreateView() => new TextTransformControl(
            "Enter a URL to parse",
            "Result",
            new[]
            {
                new TextTransformAction("Parse URL", UrlParserService.Parse, Primary: true)
            });
    }
}
