using DevToolbox.Core;

namespace DevToolbox.Tools.PasswordGenerator
{
    public class PasswordGeneratorTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Password Generator";
        public string Description => "Generates random passwords or word-based passphrases with a live strength estimate.";

        /// <summary>Creates a fresh Password Generator view instance for the shell's content area.</summary>
        public Control CreateView() => new PasswordGeneratorControl();
    }
}
