using DevToolbox.Tools.JsonFormatter;
using DevToolbox.UI;

namespace DevToolbox.Tools.CodeRunner
{
    public enum CodeTokenKind { Text, String, Comment, Number }

    public readonly record struct CodeSegment(string Text, CodeTokenKind Kind);

    /// <summary>
    /// A single lightweight, generic syntax highlighter shared across the 7 non-markup languages
    /// (PowerShell, Python, JavaScript, Batch, Java, R, C, C++) - deliberately NOT 7 separate
    /// per-language grammars. It only recognizes 3 things common to all of them: string literals
    /// ('...' or "..."), a single-line comment (using whichever marker(s) the caller passes for
    /// the current language - "#", "//", or Batch's "REM"/"::"), and numeric literals. Everything
    /// else stays the normal text color. This is "good enough to look intentional and colorful",
    /// not a real tokenizer/parser - it doesn't understand escape-sequence edge cases, multi-line
    /// strings, block comments, or per-language keyword lists.
    /// </summary>
    public static class GenericCodeHighlighter
    {
        public static List<CodeSegment> Tokenize(string code, IReadOnlyList<string> lineCommentMarkers)
        {
            code ??= string.Empty;
            var segments = new List<CodeSegment>();
            var pos = 0;
            var atLineStart = true;
            var textStart = 0;

            void FlushText(int end)
            {
                if (end > textStart) segments.Add(new CodeSegment(code.Substring(textStart, end - textStart), CodeTokenKind.Text));
            }

            while (pos < code.Length)
            {
                var c = code[pos];

                if (c == '\n')
                {
                    atLineStart = true;
                    pos++;
                    continue;
                }

                if (atLineStart && char.IsWhiteSpace(c))
                {
                    // Leading indentation before a possible comment marker - stays plain text,
                    // doesn't affect the "are we at the start of a line" check below.
                    pos++;
                    continue;
                }

                var commentMarker = atLineStart ? MatchCommentMarker(code, pos, lineCommentMarkers) : null;
                if (commentMarker is not null)
                {
                    FlushText(pos);
                    var end = code.IndexOf('\n', pos);
                    if (end < 0) end = code.Length;
                    segments.Add(new CodeSegment(code.Substring(pos, end - pos), CodeTokenKind.Comment));
                    pos = end;
                    textStart = pos;
                    atLineStart = false;
                    continue;
                }

                if (c is '"' or '\'')
                {
                    FlushText(pos);
                    var end = ScanString(code, pos);
                    segments.Add(new CodeSegment(code.Substring(pos, end - pos), CodeTokenKind.String));
                    pos = end;
                    textStart = pos;
                    atLineStart = false;
                    continue;
                }

                if (char.IsDigit(c) && !IsPartOfWord(code, pos))
                {
                    FlushText(pos);
                    var end = ScanNumber(code, pos);
                    segments.Add(new CodeSegment(code.Substring(pos, end - pos), CodeTokenKind.Number));
                    pos = end;
                    textStart = pos;
                    atLineStart = false;
                    continue;
                }

                if (!char.IsWhiteSpace(c)) atLineStart = false;
                pos++;
            }

            FlushText(code.Length);
            return segments;
        }

        // A digit immediately preceded by a letter/underscore is part of an identifier (e.g. the
        // "1" in "var1") rather than the start of its own numeric literal.
        private static bool IsPartOfWord(string code, int pos) => pos > 0 && (char.IsLetter(code[pos - 1]) || code[pos - 1] == '_');

        private static string? MatchCommentMarker(string code, int pos, IReadOnlyList<string> markers)
        {
            foreach (var marker in markers)
            {
                if (pos + marker.Length > code.Length) continue;
                if (string.Compare(code, pos, marker, 0, marker.Length, StringComparison.OrdinalIgnoreCase) != 0) continue;

                // A marker starting with a letter (Batch's "REM") only counts as a comment when
                // it's a whole word - "REM" must not match the start of "REMOVE-ITEM" or similar.
                // Symbolic markers ("#", "//", "::") have no such word-boundary concept.
                if (char.IsLetter(marker[0]))
                {
                    var afterIdx = pos + marker.Length;
                    if (afterIdx < code.Length && (char.IsLetterOrDigit(code[afterIdx]) || code[afterIdx] == '_')) continue;
                }

                return marker;
            }
            return null;
        }

        private static int ScanString(string code, int start)
        {
            var quote = code[start];
            var pos = start + 1;
            while (pos < code.Length && code[pos] != quote)
            {
                // Best-effort backslash-escape handling (covers \" \\ etc. in most of these
                // languages) - not correct for every language's exact escaping rules, but good
                // enough to stop a lone escaped quote from prematurely ending the string segment.
                if (code[pos] == '\\' && pos + 1 < code.Length) pos++;
                pos++;
            }
            if (pos < code.Length) pos++; // include the closing quote
            return pos;
        }

        private static int ScanNumber(string code, int start)
        {
            var pos = start;
            while (pos < code.Length && (char.IsLetterOrDigit(code[pos]) || code[pos] == '.')) pos++;
            return pos;
        }
    }

    /// <summary>
    /// Wires GenericCodeHighlighter (or, for HTML, the app's existing MarkupHighlighter) up to a
    /// RichTextBox the same way JsonHighlighter/MarkupHighlighter do for every other tool's live
    /// input pane: recolor on every keystroke, preserving caret/scroll position across the rebuild.
    /// </summary>
    internal static class CodeRunnerHighlighter
    {
        // Which marker(s) count as a line comment for each non-HTML language, keyed by
        // LanguageDefinition.Name. Batch gets two: "REM" (a whole word) and "::" (the common
        // label-as-comment convention almost every real-world .bat file actually uses).
        private static readonly Dictionary<string, string[]> CommentMarkers = new()
        {
            ["PowerShell"] = new[] { "#" },
            ["Python"] = new[] { "#" },
            ["JavaScript (Node.js)"] = new[] { "//" },
            ["Batch (cmd)"] = new[] { "REM", "::" },
            ["Java"] = new[] { "//" },
            ["R"] = new[] { "#" },
            ["C"] = new[] { "//" },
            ["C++"] = new[] { "//" },
        };

        public static void Highlight(RichTextBox rtb, LanguageDefinition? language)
        {
            var selectionStart = rtb.SelectionStart;
            var selectionLength = rtb.SelectionLength;
            var scrollPos = NativeMethods.GetScrollPos(rtb);

            NativeMethods.SuspendDrawing(rtb);
            try
            {
                if (language?.Kind == LanguageKind.OpenInBrowser)
                {
                    // HTML already has a real tag-aware tokenizer in this app - reuse it rather
                    // than running it through the generic 3-token highlighter below.
                    ApplyMarkup(rtb);
                }
                else
                {
                    ApplyGeneric(rtb, language);
                }

                rtb.Select(selectionStart, selectionLength);
                NativeMethods.SetScrollPos(rtb, scrollPos);
            }
            finally
            {
                NativeMethods.ResumeDrawing(rtb);
            }
        }

        private static void ApplyMarkup(RichTextBox rtb)
        {
            var segments = MarkupSyntaxTokenizer.Tokenize(rtb.Text);
            rtb.SelectAll();
            rtb.SelectionColor = Theme.Text;

            var pos = 0;
            foreach (var segment in segments)
            {
                if (segment.Text.Length > 0)
                {
                    rtb.Select(pos, segment.Text.Length);
                    rtb.SelectionColor = MarkupSyntaxColors.For(segment.Kind);
                }
                pos += segment.Text.Length;
            }
        }

        private static void ApplyGeneric(RichTextBox rtb, LanguageDefinition? language)
        {
            var markers = language is not null && CommentMarkers.TryGetValue(language.Name, out var m) ? m : Array.Empty<string>();
            var segments = GenericCodeHighlighter.Tokenize(rtb.Text, markers);

            rtb.SelectAll();
            rtb.SelectionColor = Theme.Text;

            var pos = 0;
            foreach (var segment in segments)
            {
                if (segment.Text.Length > 0)
                {
                    rtb.Select(pos, segment.Text.Length);
                    rtb.SelectionColor = ColorFor(segment.Kind);
                }
                pos += segment.Text.Length;
            }
        }

        // Reuses the same semantic colors JsonColors/MarkupSyntaxColors already use for the
        // equivalent concepts (string -> string-value color, comment -> muted, number -> number
        // color), so this stays visually consistent with every other syntax-highlighted pane in
        // the app rather than inventing a fourth palette.
        private static Color ColorFor(CodeTokenKind kind) => kind switch
        {
            CodeTokenKind.String => JsonColors.For(JsonTokenKind.StringValue),
            CodeTokenKind.Comment => Theme.TextMuted,
            CodeTokenKind.Number => JsonColors.For(JsonTokenKind.Number),
            _ => Theme.Text
        };
    }
}
