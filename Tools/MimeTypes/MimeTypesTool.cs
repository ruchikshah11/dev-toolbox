using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.MimeTypes
{
    public class MimeTypesTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "List of MIME Types";
        public string Description => "Searchable reference of common file extensions and their standard MIME type.";

        public Control CreateView() => new ReferenceTableControl(
            "List of MIME Types",
            new[] { "Extension", "MIME Type" },
            MimeTypeData.Rows);
    }
}
