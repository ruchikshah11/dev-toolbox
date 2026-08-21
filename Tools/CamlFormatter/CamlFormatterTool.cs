using DevToolbox.Core;
using DevToolbox.Tools.XmlFormatter;
using DevToolbox.UI;

namespace DevToolbox.Tools.CamlFormatter
{
    /// <summary>
    /// CAML is just XML, so this reuses XmlFormatterService directly instead of re-implementing
    /// indentation logic - the only thing this tool adds is CAML-appropriate labeling/wording.
    /// </summary>
    public class CamlFormatterTool : ITool
    {
        public string Category => "SharePoint";
        public string Name => "CAML Query Formatter";
        public string Description => "Pretty-prints a CAML query (SharePoint's XML query language for lists), using the same formatting engine as the XML Formatter.";

        public Control CreateView() => new TextTransformControl(
            "Paste your CAML query (e.g. <Where><Eq>...</Eq></Where>)",
            "Formatted CAML",
            new[]
            {
                new TextTransformAction("Format (2 spaces)", xml => XmlFormatterService.Format(xml, XmlIndentStyle.TwoSpaces), Primary: true),
                new TextTransformAction("Format (4 spaces)", xml => XmlFormatterService.Format(xml, XmlIndentStyle.FourSpaces)),
                new TextTransformAction("Compact (1 line)", xml => XmlFormatterService.Format(xml, XmlIndentStyle.Compact))
            });
    }
}
