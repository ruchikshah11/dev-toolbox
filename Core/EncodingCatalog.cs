using System.Text;

namespace DevToolbox.Core
{
    /// <summary>
    /// The set of file encodings offered when reading an uploaded file, shared by any tool
    /// that needs to read text files (JSON formatter today, future XML/CSV/text tools later).
    /// Built defensively: a candidate is only listed if this machine's code page tables
    /// actually support it, so an unavailable codepage never crashes the dropdown.
    /// </summary>
    public static class EncodingCatalog
    {
        private static readonly (string Display, string Name)[] Candidates =
        {
            ("UTF-8", "utf-8"),
            ("UTF-16 (Unicode)", "utf-16"),
            ("UTF-16 Big Endian", "utf-16BE"),
            ("UTF-32", "utf-32"),
            ("US-ASCII", "us-ascii"),
            ("Windows-1252 (Western European)", "windows-1252"),
            ("ISO-8859-1 (Latin Alphabet No. 1)", "iso-8859-1"),
            ("ISO-8859-2 (Latin Alphabet No. 2)", "iso-8859-2"),
            ("ISO-8859-3 (Latin Alphabet No. 3)", "iso-8859-3"),
            ("ISO-8859-4 (Latin Alphabet No. 4)", "iso-8859-4"),
            ("ISO-8859-5 (Latin/Cyrillic Alphabet)", "iso-8859-5"),
            ("ISO-8859-6 (Latin/Arabic Alphabet)", "iso-8859-6"),
            ("ISO-8859-7 (Latin/Greek Alphabet)", "iso-8859-7"),
            ("ISO-8859-8 (Latin/Hebrew Alphabet)", "iso-8859-8"),
            ("ISO-8859-9 (Latin Alphabet No. 5, Turkish)", "iso-8859-9"),
            ("ISO-8859-13 (Latin Alphabet No. 7, Baltic)", "iso-8859-13"),
            ("ISO-8859-15 (Latin Alphabet No. 9)", "iso-8859-15"),
        };

        public static IReadOnlyList<(string Display, Encoding Encoding)> Available { get; } = Build();

        public static Encoding Default => Available.FirstOrDefault(a => a.Display == "UTF-8").Encoding ?? Encoding.UTF8;

        private static List<(string, Encoding)> Build()
        {
            var list = new List<(string, Encoding)>();
            foreach (var (display, name) in Candidates)
            {
                try
                {
                    list.Add((display, Encoding.GetEncoding(name)));
                }
                catch (ArgumentException)
                {
                    // Codepage not registered on this machine - just omit it from the list.
                }
            }
            return list;
        }
    }
}
