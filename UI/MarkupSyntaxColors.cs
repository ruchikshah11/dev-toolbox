namespace DevToolbox.UI
{
    // Same palette values as JsonColors (JsonFormatter), reused for analogous roles so every
    // syntax-highlighted editor/output in the app reads consistently.
    internal static class MarkupSyntaxColors
    {
        public static Color TagBracket => Theme.IsDark ? ColorTranslator.FromHtml("#D7DBE5") : ColorTranslator.FromHtml("#1C2233");
        public static Color TagName => Theme.IsDark ? ColorTranslator.FromHtml("#7FA8FF") : ColorTranslator.FromHtml("#1A56DB");
        public static Color AttributeName => Theme.IsDark ? ColorTranslator.FromHtml("#F0A857") : ColorTranslator.FromHtml("#B45309");
        public static Color AttributeValue => Theme.IsDark ? ColorTranslator.FromHtml("#3ED68C") : ColorTranslator.FromHtml("#047857");
        public static Color Comment => Theme.IsDark ? ColorTranslator.FromHtml("#7FBF7F") : ColorTranslator.FromHtml("#6A9955");
        public static Color Doctype => Theme.IsDark ? ColorTranslator.FromHtml("#9BA3B4") : ColorTranslator.FromHtml("#6B7280");

        public static Color For(MarkupTokenKind kind) => kind switch
        {
            MarkupTokenKind.TagBracket => TagBracket,
            MarkupTokenKind.TagName => TagName,
            MarkupTokenKind.AttributeName => AttributeName,
            MarkupTokenKind.AttributeEquals => TagBracket,
            MarkupTokenKind.AttributeValue => AttributeValue,
            MarkupTokenKind.Comment => Comment,
            MarkupTokenKind.Doctype => Doctype,
            _ => Theme.Text
        };
    }
}
