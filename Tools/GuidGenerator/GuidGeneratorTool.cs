using DevToolbox.Core;

namespace DevToolbox.Tools.GuidGenerator
{
    public class GuidGeneratorTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "GUID Generator";
        public string Description => "Generates random GUIDs (UUID v4) with configurable hyphens, case, and braces.";

        public Control CreateView() => new GuidGeneratorControl();
    }
}
