namespace DevToolbox.Tools.LoremIpsum
{
    /// <summary>
    /// Pure text-generation logic for the Lorem Ipsum Generator tool: no WinForms references so
    /// it can be unit tested / reused on its own, matching the Service/Control split used by the
    /// other tools in this project.
    /// </summary>
    public static class LoremIpsumService
    {
        // The classic ~69-word Lorem Ipsum word bank (derived from Cicero's "de Finibus Bonorum
        // et Malorum"), the same pool every well-known Lorem Ipsum generator draws from.
        private static readonly string[] WordBank =
        {
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do",
            "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim",
            "ad", "minim", "veniam", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip",
            "ex", "ea", "commodo", "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate",
            "velit", "esse", "cillum", "eu", "fugiat", "nulla", "pariatur", "excepteur", "sint", "occaecat",
            "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia", "deserunt", "mollit", "anim",
            "id", "est", "laborum"
        };

        private const string TraditionalOpeningSentence =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";

        public static string Generate(int paragraphCount, bool startWithTraditionalOpening)
        {
            if (paragraphCount < 1) paragraphCount = 1;

            var random = new Random();
            var paragraphs = new List<string>(paragraphCount);

            for (var p = 0; p < paragraphCount; p++)
            {
                var sentenceCount = random.Next(3, 7); // 3-6 sentences per paragraph
                var sentences = new List<string>(sentenceCount);

                if (p == 0 && startWithTraditionalOpening)
                {
                    sentences.Add(TraditionalOpeningSentence);
                    sentenceCount--; // the forced opening counts as one of the paragraph's sentences
                }

                for (var s = 0; s < sentenceCount; s++)
                {
                    sentences.Add(GenerateSentence(random));
                }

                paragraphs.Add(string.Join(" ", sentences));
            }

            return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
        }

        private static string GenerateSentence(Random random)
        {
            var wordCount = random.Next(4, 15); // 4-14 words
            var words = new string[wordCount];
            for (var i = 0; i < wordCount; i++)
            {
                words[i] = WordBank[random.Next(WordBank.Length)];
            }

            var sentence = string.Join(" ", words);
            sentence = char.ToUpperInvariant(sentence[0]) + sentence.Substring(1);
            return sentence + ".";
        }
    }
}
