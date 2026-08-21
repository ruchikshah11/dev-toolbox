using System.Text;

namespace DevToolbox.Tools.JavaScriptEscape
{
    public static class JavaScriptEscapeService
    {
        // Produces the BODY of a JavaScript string literal (no surrounding quotes added).
        // Both quote characters are escaped since JS string literals may use either.
        public static string Escape(string input)
        {
            input ??= string.Empty;
            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\'': sb.Append("\\'"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        // Reverses Escape. Lenient: an unrecognized backslash sequence is left as-is rather
        // than throwing.
        public static string Unescape(string input)
        {
            input ??= string.Empty;
            var sb = new StringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (c == '\\' && i + 1 < input.Length)
                {
                    var next = input[i + 1];
                    switch (next)
                    {
                        case '\\': sb.Append('\\'); i++; continue;
                        case '"': sb.Append('"'); i++; continue;
                        case '\'': sb.Append('\''); i++; continue;
                        case 'n': sb.Append('\n'); i++; continue;
                        case 'r': sb.Append('\r'); i++; continue;
                        case 't': sb.Append('\t'); i++; continue;
                        default: sb.Append(c); continue; // leave unrecognized escape as-is
                    }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
