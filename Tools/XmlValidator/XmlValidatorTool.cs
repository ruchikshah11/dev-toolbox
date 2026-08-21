using DevToolbox.Core;

namespace DevToolbox.Tools.XmlValidator
{
    public class XmlValidatorTool : ITool
    {
        public string Category => "Validators";
        public string Name => "XML Validator";
        public string Description => "Validates an XML document against well-formedness rules or an XSD schema.";

        public Control CreateView() => new XmlValidatorControl();
    }
}
