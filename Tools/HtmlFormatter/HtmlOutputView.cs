using DevToolbox.UI;
using HtmlAgilityPack;

namespace DevToolbox.Tools.HtmlFormatter
{
    /// <summary>
    /// Displays formatted HTML two ways - as colorized text, and as a collapsible, colorized DOM
    /// tree - via the shared TabbedOutputView scaffolding (mirrors JsonFormatter's JsonOutputView
    /// and XmlFormatter's XmlOutputView).
    /// </summary>
    internal sealed class HtmlOutputView : TabbedOutputView
    {
        public void Render(string formattedText, HtmlNode? documentNode)
        {
            SetFormattedText(formattedText);
            SetTreeRoots(documentNode is null ? Enumerable.Empty<TreeNode>() : HtmlTreeBuilder.BuildRoots(documentNode));
        }
    }
}
