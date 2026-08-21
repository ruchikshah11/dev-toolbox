namespace DevToolbox.Tools.SqlEscape
{
    public static class SqlEscapeService
    {
        // Doubles single quotes for safe use inside a SQL string literal (content only, no
        // added outer quotes).
        public static string Escape(string input) => (input ?? string.Empty).Replace("'", "''");

        public static string Unescape(string input) => (input ?? string.Empty).Replace("''", "'");
    }
}
