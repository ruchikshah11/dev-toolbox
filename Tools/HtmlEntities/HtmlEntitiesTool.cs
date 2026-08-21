using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.HtmlEntities
{
    public class HtmlEntitiesTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "HTML Entities";
        public string Description => "Searchable reference of standard HTML5 named character references and the characters they represent.";

        public Control CreateView() => new ReferenceTableControl(
            "HTML Entities",
            new[] { "Entity", "Character", "Description" },
            HtmlEntityData.Rows);
    }
}
