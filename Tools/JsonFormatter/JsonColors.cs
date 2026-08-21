using DevToolbox.UI;

namespace DevToolbox.Tools.JsonFormatter
{
    // Colors for syntax highlighting, shared by the "Formatted Text" and "Tree View" outputs
    // so both represent the same JSON the same way. Reads Theme.IsDark on every access (rather
    // than caching a fixed palette) since the near-black "Structural" color from light mode
    // would be unreadable against a dark card background.
    internal static class JsonColors
    {
        public static Color Structural => Theme.IsDark ? ColorTranslator.FromHtml("#D7DBE5") : ColorTranslator.FromHtml("#1C2233");
        public static Color Key => Theme.IsDark ? ColorTranslator.FromHtml("#7FA8FF") : ColorTranslator.FromHtml("#1A56DB");
        public static Color StringValue => Theme.IsDark ? ColorTranslator.FromHtml("#3ED68C") : ColorTranslator.FromHtml("#047857");
        public static Color Number => Theme.IsDark ? ColorTranslator.FromHtml("#F0A857") : ColorTranslator.FromHtml("#B45309");
        public static Color Boolean => Theme.IsDark ? ColorTranslator.FromHtml("#B69CFF") : ColorTranslator.FromHtml("#7C3AED");
        public static Color Null => Theme.IsDark ? ColorTranslator.FromHtml("#9BA3B4") : ColorTranslator.FromHtml("#6B7280");
        public static Color ContainerSummary => Null;
        public static Color Comment => Theme.IsDark ? ColorTranslator.FromHtml("#7FBF7F") : ColorTranslator.FromHtml("#6A9955");

        public static Color For(JsonTokenKind kind) => kind switch
        {
            JsonTokenKind.Key => Key,
            JsonTokenKind.StringValue => StringValue,
            JsonTokenKind.Number => Number,
            JsonTokenKind.Boolean => Boolean,
            JsonTokenKind.Null => Null,
            JsonTokenKind.Comment => Comment,
            _ => Structural
        };
    }
}
