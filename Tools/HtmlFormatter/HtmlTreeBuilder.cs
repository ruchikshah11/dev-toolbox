using DevToolbox.UI;
using HtmlAgilityPack;

namespace DevToolbox.Tools.HtmlFormatter
{
    // Builds the collapsible TreeView representation of a parsed HTML document, mirroring
    // XmlFormatter's XmlTreeBuilder. Unlike XML (always one root element), an HTML document can
    // have several top-level nodes, so this returns a list of roots rather than a single one.
    internal static class HtmlTreeBuilder
    {
        public static List<TreeNode> BuildRoots(HtmlNode documentNode) =>
            documentNode.ChildNodes
                .Where(IsMeaningful)
                .Select(BuildNode)
                .ToList();

        private static bool IsMeaningful(HtmlNode node) =>
            node.NodeType is HtmlNodeType.Element or HtmlNodeType.Comment
            || (node.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(node.InnerText));

        private static TreeNode BuildNode(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Comment)
            {
                return MarkupTreeView.MakeNode(node.OuterHtml.Trim(), "", "", MarkupSyntaxColors.Comment, MarkupSyntaxColors.Comment);
            }

            if (node.NodeType == HtmlNodeType.Text)
            {
                var text = node.InnerText.Trim();
                return MarkupTreeView.MakeNode(text, "", "", MarkupSyntaxColors.AttributeValue, MarkupSyntaxColors.AttributeValue);
            }

            var tagLabel = "<" + node.Name + ">";
            if (node.Attributes.Count > 0)
            {
                tagLabel += "  " + string.Join(" ", node.Attributes.Select(a => $"{a.Name}=\"{a.Value}\""));
            }

            var children = node.ChildNodes.Where(IsMeaningful).ToList();

            string separator;
            string valueText;
            Color valueColor;

            if (children.Count > 0)
            {
                separator = "  ";
                valueText = $"{{ {children.Count} {(children.Count == 1 ? "child node" : "child nodes")} }}";
                valueColor = MarkupSyntaxColors.Doctype;
            }
            else
            {
                separator = "";
                valueText = "";
                valueColor = MarkupSyntaxColors.AttributeValue;
            }

            var treeNode = MarkupTreeView.MakeNode(tagLabel, separator, valueText, MarkupSyntaxColors.TagName, valueColor);
            foreach (var child in children)
            {
                treeNode.Nodes.Add(BuildNode(child));
            }

            return treeNode;
        }
    }
}
