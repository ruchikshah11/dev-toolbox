namespace DevToolbox.Tools.GuidGenerator
{
    public static class GuidGeneratorService
    {
        public static string Generate(int count, bool uppercase, bool hyphens, bool braces)
        {
            if (count < 1)
            {
                throw new FormatException("Enter a quantity of at least 1.");
            }
            if (count > 1000)
            {
                throw new FormatException("Generate at most 1000 GUIDs at a time.");
            }

            var lines = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                var text = Guid.NewGuid().ToString(hyphens ? "D" : "N");
                if (uppercase) text = text.ToUpperInvariant();
                if (braces) text = "{" + text + "}";
                lines.Add(text);
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
