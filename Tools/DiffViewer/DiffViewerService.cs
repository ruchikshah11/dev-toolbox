using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevToolbox.Tools.DiffViewer
{
    public enum DiffLineKind { Unchanged, Added, Removed }

    // OldLineNumber/NewLineNumber are 1-based, and null on the side a line doesn't exist on (an
    // Added line has no position in the original, a Removed line has no position in the changed
    // text) - the same two-column line-number gutter convention most diff tools use.
    public readonly record struct DiffLine(string Text, DiffLineKind Kind, int? OldLineNumber, int? NewLineNumber);

    public static class DiffViewerService
    {
        // The LCS table below is O(n*m) cells - fine for pasted snippets, but this guards
        // against accidentally diffing two huge files and exhausting memory.
        private const int MaxCells = 4_000_000;

        public static List<DiffLine> ComputeLineDiff(string left, string right) =>
            Diff(SplitLines(left), SplitLines(right));

        public static List<DiffLine> ComputeJsonDiff(string left, string right) =>
            Diff(SplitLines(PrettyPrintJson(left, "left")), SplitLines(PrettyPrintJson(right, "right")));

        public static List<DiffLine> ComputeXmlDiff(string left, string right) =>
            Diff(SplitLines(PrettyPrintXml(left, "left")), SplitLines(PrettyPrintXml(right, "right")));

        private static string PrettyPrintJson(string json, string side)
        {
            try
            {
                return JToken.Parse(json ?? string.Empty).ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (JsonReaderException ex)
            {
                throw new FormatException($"The {side} side is not valid JSON: {ex.Message}", ex);
            }
        }

        private static string PrettyPrintXml(string xml, string side)
        {
            try
            {
                return XDocument.Parse(xml ?? string.Empty).ToString(SaveOptions.None);
            }
            catch (XmlException ex)
            {
                throw new FormatException($"The {side} side is not valid XML: {ex.Message}", ex);
            }
        }

        private static string[] SplitLines(string text) =>
            (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        private static List<DiffLine> Diff(string[] a, string[] b)
        {
            var n = a.Length;
            var m = b.Length;
            if ((long)(n + 1) * (m + 1) > MaxCells)
            {
                throw new FormatException("Input is too large to diff here - this tool is for comparing snippets, not full files.");
            }

            var lcs = new int[n + 1, m + 1];
            for (var i = n - 1; i >= 0; i--)
            {
                for (var j = m - 1; j >= 0; j--)
                {
                    lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }

            var result = new List<DiffLine>();
            var x = 0;
            var y = 0;
            while (x < n && y < m)
            {
                if (a[x] == b[y])
                {
                    result.Add(new DiffLine(a[x], DiffLineKind.Unchanged, x + 1, y + 1));
                    x++; y++;
                }
                else if (lcs[x + 1, y] >= lcs[x, y + 1])
                {
                    result.Add(new DiffLine(a[x], DiffLineKind.Removed, x + 1, null));
                    x++;
                }
                else
                {
                    result.Add(new DiffLine(b[y], DiffLineKind.Added, null, y + 1));
                    y++;
                }
            }
            while (x < n) { result.Add(new DiffLine(a[x], DiffLineKind.Removed, x + 1, null)); x++; }
            while (y < m) { result.Add(new DiffLine(b[y], DiffLineKind.Added, null, y + 1)); y++; }

            return result;
        }
    }
}
