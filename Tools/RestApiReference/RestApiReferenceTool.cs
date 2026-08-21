using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.RestApiReference
{
    public class RestApiReferenceTool : ITool
    {
        public string Category => "SharePoint";
        public string Name => "REST API Query Reference";
        public string Description => "Searchable reference of common SharePoint REST/OData endpoints and query parameters, with example URLs.";

        public Control CreateView() => new ReferenceTableControl(
            "SharePoint REST API Query Reference",
            new[] { "API Area", "Operation", "Method", "Endpoint / Example", "Notes" },
            RestApiReferenceData.Rows);
    }
}
