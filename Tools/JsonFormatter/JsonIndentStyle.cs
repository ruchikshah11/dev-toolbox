namespace DevToolbox.Tools.JsonFormatter
{
    public enum JsonIndentStyle
    {
        TwoSpaces,
        ThreeSpaces,
        FourSpaces,
        Tab,
        Compact,
        JavaScriptEscaped
    }

    public enum JsonBracketStyle
    {
        // "key": { ... on the same line as the key/colon (K&R style).
        Collapsed,

        // "key":
        // { ... brace on its own new line (Allman style).
        Expanded
    }
}
