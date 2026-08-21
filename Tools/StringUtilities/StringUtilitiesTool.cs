using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.StringUtilities
{
    public class StringUtilitiesTool : ITool
    {
        public string Category => "String Escaper & Utilities";
        public string Name => "String Utilities";
        public string Description => "Case conversion (prose and camelCase/PascalCase/snake_case/kebab-case), URL slug generation, whitespace cleanup, reversal, blank-line removal and stats for plain text.";

        /// <summary>Wires every String Utilities action into the shared paste-in/run/see-result shell, as a dropdown given how many actions there are.</summary>
        public Control CreateView() => new TextTransformControl(
            "Enter the text to transform",
            "Result",
            new[]
            {
                new TextTransformAction("UPPERCASE", StringUtilitiesService.ToUpper, Primary: true),
                new TextTransformAction("lowercase", StringUtilitiesService.ToLower),
                new TextTransformAction("Title Case", StringUtilitiesService.ToTitleCase),
                new TextTransformAction("camelCase", StringUtilitiesService.ToCamelCase),
                new TextTransformAction("PascalCase", StringUtilitiesService.ToPascalCase),
                new TextTransformAction("snake_case", StringUtilitiesService.ToSnakeCase),
                new TextTransformAction("kebab-case", StringUtilitiesService.ToKebabCase),
                new TextTransformAction("URL Slug", StringUtilitiesService.ToSlug),
                // A plain single "&" is fine here (unlike on a Button) - ComboBox item text is
                // rendered via ToString(), which doesn't apply Windows' "&" mnemonic-prefix
                // handling the way Button.Text/Label.Text with UseMnemonic does.
                new TextTransformAction("Trim & Collapse Whitespace", StringUtilitiesService.TrimAndCollapseWhitespace),
                new TextTransformAction("Reverse", StringUtilitiesService.Reverse),
                new TextTransformAction("Remove Blank Lines", StringUtilitiesService.RemoveBlankLines),
                new TextTransformAction("Stats", StringUtilitiesService.Stats)
            },
            useDropdownSelector: true);
    }
}
