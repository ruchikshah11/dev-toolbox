using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.I18nStandards
{
    public class I18nStandardsTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "I18N Standards / Locale Codes";
        public string Description => "Searchable reference of culture codes, language codes and regions, sourced live from the .NET runtime's culture list.";

        public Control CreateView() => new ReferenceTableControl(
            "I18N Standards - Culture & Locale Codes",
            new[] { "Culture Code", "Culture Name", "Language Code", "Region" },
            I18nStandardsData.GetRows());
    }
}
