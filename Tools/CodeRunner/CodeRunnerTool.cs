using DevToolbox.Core;

namespace DevToolbox.Tools.CodeRunner
{
    public class CodeRunnerTool : ITool
    {
        public string Category => "Code Runner";
        public string Name => "Code Runner";
        public string Description => "Runs code using your locally-installed PowerShell, Python, Node.js, cmd.exe, Java, R, GCC/G++ (C/C++), or opens HTML in your browser. Executes directly on this machine (no sandboxing) using whichever of those toolchains it can find on PATH.";

        public Control CreateView() => new CodeRunnerControl();
    }
}
