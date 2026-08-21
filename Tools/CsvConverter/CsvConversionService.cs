using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace DevToolbox.Tools.CsvConverter
{
    public static class CsvConversionService
    {
        public static string CsvToJson(string csv)
        {
            var rows = ParseRecords(csv);
            return JsonConvert.SerializeObject(rows, Formatting.Indented);
        }

        public static string CsvToXml(string csv)
        {
            var rows = ParseRecords(csv);

            var root = new XElement("Root");
            foreach (var row in rows)
            {
                var rowElement = new XElement("Row");
                foreach (var kv in row)
                {
                    rowElement.Add(new XElement(SanitizeElementName(kv.Key), kv.Value ?? string.Empty));
                }
                root.Add(rowElement);
            }

            return new XDocument(root).ToString();
        }

        // Parses raw CSV into a header row + one Dictionary<string,string> per data row
        // (columns keep their original order since plain inserts into a Dictionary<TKey,TValue>
        // are enumerated in insertion order in practice on .NET).
        public static List<Dictionary<string, string>> ParseRecords(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                throw new FormatException("Nothing to convert - paste some CSV content first.");
            }

            var rawRows = ParseRows(csv);
            if (rawRows.Count == 0)
            {
                throw new FormatException("The CSV has no content to convert.");
            }

            var headers = rawRows[0];
            if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
            {
                throw new FormatException("The CSV is missing a header row.");
            }

            var records = new List<Dictionary<string, string>>();
            for (var r = 1; r < rawRows.Count; r++)
            {
                var fields = rawRows[r];
                var record = new Dictionary<string, string>();
                for (var c = 0; c < headers.Count; c++)
                {
                    var header = string.IsNullOrWhiteSpace(headers[c]) ? $"Column{c + 1}" : headers[c];
                    var value = c < fields.Count ? fields[c] : string.Empty;
                    record[header] = value;
                }
                records.Add(record);
            }

            return records;
        }

        // A small hand-rolled CSV reader that understands quoted fields (RFC 4180 style):
        // commas/newlines inside "..." are part of the field, and "" inside a quoted field is
        // an escaped literal quote.
        private static List<List<string>> ParseRows(string csv)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var rowHasContent = false;
            var i = 0;
            var len = csv.Length;

            while (i < len)
            {
                var c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < len && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    field.Append(c);
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                    rowHasContent = true;
                    i++;
                    continue;
                }

                if (c == ',')
                {
                    currentRow.Add(field.ToString());
                    field.Clear();
                    rowHasContent = true;
                    i++;
                    continue;
                }

                if (c == '\r')
                {
                    i++;
                    continue;
                }

                if (c == '\n')
                {
                    currentRow.Add(field.ToString());
                    field.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    rowHasContent = false;
                    i++;
                    continue;
                }

                field.Append(c);
                rowHasContent = true;
                i++;
            }

            if (inQuotes)
            {
                throw new FormatException("Invalid CSV: a quoted field is never closed (missing a trailing \").");
            }

            if (field.Length > 0 || currentRow.Count > 0 || rowHasContent)
            {
                currentRow.Add(field.ToString());
                rows.Add(currentRow);
            }

            // Trailing blank line(s) (a lone empty field) are just formatting, not data.
            while (rows.Count > 0 && rows[rows.Count - 1].Count == 1 && rows[rows.Count - 1][0].Length == 0)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            return rows;
        }

        private static string SanitizeElementName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Column";

            var sb = new StringBuilder();
            foreach (var c in name)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' ? c : '_');
            }

            var result = sb.ToString();
            if (result.Length == 0) result = "Column";
            if (!char.IsLetter(result[0]) && result[0] != '_') result = "_" + result;
            return result;
        }
    }
}
