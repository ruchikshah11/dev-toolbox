using System.Text.RegularExpressions;

namespace DevToolbox.Tools.SqlFormatter
{
    /// <summary>
    /// Heuristic formatter, NOT a real SQL parser: it recognizes common keywords/clauses with
    /// regexes and reflows them onto their own, indented lines. This is good enough to turn a
    /// wall of SQL into something readable, but it does not validate syntax and can be fooled by
    /// unusual constructs (deeply nested subqueries, vendor-specific syntax, comments, etc.).
    /// </summary>
    public static class SqlFormatterService
    {
        private const string Indent = "    ";
        private const string LiteralMarkerPrefix = "@@LIT";
        private const string LiteralMarkerSuffix = "@@";

        // Longest-first so multi-word phrases (e.g. "LEFT JOIN") win over their component
        // words (e.g. "JOIN") when the keyword regex is built below.
        private static readonly string[] KeywordPhrases =
        {
            "LEFT OUTER JOIN", "RIGHT OUTER JOIN", "FULL OUTER JOIN",
            "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "FULL JOIN", "CROSS JOIN",
            "GROUP BY", "ORDER BY", "INSERT INTO", "UNION ALL",
            "SELECT", "FROM", "WHERE", "JOIN", "ON", "HAVING", "VALUES", "UPDATE",
            "SET", "DELETE", "AND", "OR", "AS", "IN", "NOT", "NULL", "LIMIT", "UNION",
            "IS", "LIKE", "BETWEEN", "DISTINCT", "TOP"
        };

        // The subset of keywords/phrases that always start a brand-new, zero-indent line.
        private static readonly HashSet<string> ClauseStarters = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "GROUP BY", "ORDER BY", "HAVING",
            "INSERT INTO", "VALUES", "UPDATE", "SET", "DELETE", "UNION", "UNION ALL", "LIMIT",
            "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "FULL JOIN", "CROSS JOIN",
            "LEFT OUTER JOIN", "RIGHT OUTER JOIN", "FULL OUTER JOIN", "JOIN", "ON"
        };

        private static readonly Regex KeywordRegex = BuildWordRegex(KeywordPhrases);
        private static readonly Regex ClauseStarterRegex = BuildWordRegex(ClauseStarters.ToArray());
        private static readonly Regex StringLiteralRegex = BuildStringLiteralRegex();
        private static readonly Regex AndOrRegex = BuildAndOrRegex();

        // Bare digits alone would collide with a real numeric literal such as "LIMIT 10", so
        // each masked string literal's index is wrapped in a marker (built from ordinary ASCII
        // punctuation, never legal inside a SQL identifier or number) that safely round-trips.
        private static readonly Regex PlaceholderRegex = BuildPlaceholderRegex();

        private static Regex BuildWordRegex(string[] phrases)
        {
            var escaped = phrases
                .OrderByDescending(p => p.Length)
                .Select(p => Regex.Escape(p).Replace(@"\ ", @"\s+"));
            var pattern = "(?<![\\w])(?:" + string.Join("|", escaped) + ")(?![\\w])";
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        private static Regex BuildStringLiteralRegex()
        {
            var singleQuoted = "'(?:[^']|'')*'";
            var doubleQuoted = "\"(?:[^\"]|\"\")*\"";
            return new Regex(singleQuoted + "|" + doubleQuoted, RegexOptions.Compiled);
        }

        private static Regex BuildAndOrRegex()
        {
            return new Regex("(?<![\\w])(AND|OR)(?![\\w])", RegexOptions.Compiled);
        }

        private static Regex BuildPlaceholderRegex()
        {
            var pattern = Regex.Escape(LiteralMarkerPrefix) + "(\\d+)" + Regex.Escape(LiteralMarkerSuffix);
            return new Regex(pattern, RegexOptions.Compiled);
        }

        public static string Format(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new FormatException("Nothing to format - paste some SQL first.");
            }

            var (masked, literals) = MaskStringLiterals(sql);

            var collapsed = Regex.Replace(masked, "\\s+", " ").Trim();
            var uppered = KeywordRegex.Replace(collapsed, m => Regex.Replace(m.Value, "\\s+", " ").ToUpperInvariant());

            var lines = SplitIntoClauseLines(uppered);
            var result = string.Join("\n", lines);

            return RestoreStringLiterals(result, literals);
        }

        private static List<string> SplitIntoClauseLines(string sql)
        {
            var matches = ClauseStarterRegex.Matches(sql).Cast<Match>().ToList();
            var segments = new List<(string Keyword, string Body)>();

            if (matches.Count == 0)
            {
                // No recognized clause keywords at all - hand the input back unchanged rather
                // than guessing at structure.
                segments.Add((string.Empty, sql));
            }
            else
            {
                if (matches[0].Index > 0)
                {
                    var leading = sql.Substring(0, matches[0].Index).Trim();
                    if (leading.Length > 0) segments.Add((string.Empty, leading));
                }

                for (var i = 0; i < matches.Count; i++)
                {
                    var start = matches[i].Index;
                    var end = i + 1 < matches.Count ? matches[i + 1].Index : sql.Length;
                    var keyword = Regex.Replace(matches[i].Value, "\\s+", " ");
                    var bodyStart = start + matches[i].Length;
                    var body = sql.Substring(bodyStart, end - bodyStart).Trim();
                    segments.Add((keyword, body));
                }
            }

            var lines = new List<string>();
            foreach (var (keyword, body) in segments)
            {
                AppendSegment(lines, keyword, body);
            }
            return lines;
        }

        private static void AppendSegment(List<string> lines, string keyword, string body)
        {
            if (keyword.Length == 0)
            {
                if (body.Length > 0) lines.Add(body);
                return;
            }

            if (body.Length == 0)
            {
                lines.Add(keyword);
                return;
            }

            switch (keyword)
            {
                case "SELECT":
                case "GROUP BY":
                case "ORDER BY":
                {
                    lines.Add(keyword);
                    var parts = SplitTopLevelByComma(body);
                    for (var i = 0; i < parts.Count; i++)
                    {
                        var suffix = i < parts.Count - 1 ? "," : "";
                        lines.Add(Indent + parts[i].Trim() + suffix);
                    }
                    break;
                }

                case "WHERE":
                case "HAVING":
                case "ON":
                {
                    var parts = SplitTopLevelByAndOr(body);
                    lines.Add(keyword + " " + parts[0].Trim());
                    for (var i = 1; i < parts.Count; i++)
                    {
                        lines.Add(Indent + parts[i].Trim());
                    }
                    break;
                }

                case "VALUES":
                case "SET":
                {
                    lines.Add(keyword);
                    var parts = SplitTopLevelByComma(body);
                    for (var i = 0; i < parts.Count; i++)
                    {
                        var suffix = i < parts.Count - 1 ? "," : "";
                        lines.Add(Indent + parts[i].Trim() + suffix);
                    }
                    break;
                }

                default:
                    lines.Add(keyword + " " + body);
                    break;
            }
        }

        private static List<string> SplitTopLevelByComma(string s)
        {
            var parts = new List<string>();
            var depth = 0;
            var start = 0;
            for (var i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;
                        break;
                    case ',' when depth == 0:
                        parts.Add(s.Substring(start, i - start));
                        start = i + 1;
                        break;
                }
            }
            parts.Add(s.Substring(start));
            return parts;
        }

        private static List<string> SplitTopLevelByAndOr(string s)
        {
            var depths = new int[s.Length];
            var depth = 0;
            for (var i = 0; i < s.Length; i++)
            {
                depths[i] = depth;
                if (s[i] == '(') depth++;
                else if (s[i] == ')') depth--;
            }

            var parts = new List<string>();
            var start = 0;
            foreach (Match m in AndOrRegex.Matches(s))
            {
                if (depths[m.Index] == 0)
                {
                    parts.Add(s.Substring(start, m.Index - start));
                    start = m.Index; // keep AND/OR attached to the front of the next part
                }
            }
            parts.Add(s.Substring(start));
            return parts;
        }

        private static (string Masked, List<string> Literals) MaskStringLiterals(string sql)
        {
            var literals = new List<string>();
            var masked = StringLiteralRegex.Replace(sql, m =>
            {
                literals.Add(m.Value);
                var index = literals.Count - 1;
                return LiteralMarkerPrefix + index + LiteralMarkerSuffix;
            });
            return (masked, literals);
        }

        private static string RestoreStringLiterals(string sql, List<string> literals) =>
            PlaceholderRegex.Replace(sql, m => literals[int.Parse(m.Groups[1].Value)]);
    }
}
