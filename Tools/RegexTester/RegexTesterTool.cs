using DevToolbox.Core;

namespace DevToolbox.Tools.RegexTester
{
    public class RegexTesterTool : ITool
    {
        public string Category => "Validators";
        public string Name => "Regular Expression Tester";
        public string Description => "Tests a .NET regular expression against sample input.";

        public Control CreateView() => new RegexTesterControl();
    }
}
