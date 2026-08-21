using DevToolbox.Core;

namespace DevToolbox.Tools.XPathTester
{
    public class XPathTesterTool : ITool
    {
        public string Category => "Validators";
        public string Name => "XPath Tester";
        public string Description => "Evaluates an XPath expression against an XML document.";

        public Control CreateView() => new XPathTesterControl();
    }
}
