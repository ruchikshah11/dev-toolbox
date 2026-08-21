using DevToolbox.Core;

namespace DevToolbox.Tools.XsltTransformer
{
    public class XsltTransformerTool : ITool
    {
        public string Category => "Converters";
        public string Name => "XSL Transformer";
        public string Description => "Transforms an XML document using an XSLT stylesheet.";

        public Control CreateView() => new XsltTransformerControl();
    }
}
