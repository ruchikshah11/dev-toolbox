using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.HttpStatusCodes
{
    public class HttpStatusCodesTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "HTTP Status Codes";
        public string Description => "Searchable reference of HTTP status codes, their reason phrase, and what they mean.";

        public Control CreateView() => new ReferenceTableControl(
            "HTTP Status Codes",
            new[] { "Code", "Reason Phrase", "Meaning" },
            HttpStatusCodeData.Rows);
    }
}
