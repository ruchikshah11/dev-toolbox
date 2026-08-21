namespace DevToolbox.Tools.JsonFormatter
{
    public enum JsonTokenKind
    {
        Structural,
        Key,
        StringValue,
        Number,
        Boolean,
        Null,
        Whitespace,
        Comment
    }

    // One lexical run of the formatted output, tagged with what it represents so a UI can
    // colorize it without re-parsing the text.
    public readonly record struct JsonSegment(string Text, JsonTokenKind Kind);
}
