using DevToolbox.Core;

namespace DevToolbox.Tools.LoremIpsum
{
    public class LoremIpsumTool : ITool
    {
        public string Category => "Web Resources";
        public string Name => "Lorem Ipsum Generator";
        public string Description => "Generates placeholder Lorem Ipsum paragraphs for mockups, layouts and tests.";

        public Control CreateView() => new LoremIpsumControl();
    }
}
