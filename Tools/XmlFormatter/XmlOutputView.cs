using System.Xml.Linq;
using DevToolbox.UI;

namespace DevToolbox.Tools.XmlFormatter
{
    /// <summary>
    /// Displays formatted XML two ways - as colorized text, and as a collapsible, colorized
    /// element tree - via the shared TabbedOutputView scaffolding (mirrors JsonFormatter's
    /// JsonOutputView).
    /// </summary>
    internal sealed class XmlOutputView : TabbedOutputView
    {
        public void Render(string formattedText, XElement? root)
        {
            SetFormattedText(formattedText);
            SetTreeRoots(root is null ? Enumerable.Empty<TreeNode>() : new[] { XmlTreeBuilder.BuildNode(root) });
        }
    }
}
