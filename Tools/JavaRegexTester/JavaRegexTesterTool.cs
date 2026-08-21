using DevToolbox.Core;

namespace DevToolbox.Tools.JavaRegexTester
{
    public class JavaRegexTesterTool : ITool
    {
        public string Category => "Validators";
        public string Name => "Java Regular Expression Tester";
        public string Description => "Tests a Java-flavored regular expression against sample input.";

        public Control CreateView() => new JavaRegexTesterControl();
    }
}
