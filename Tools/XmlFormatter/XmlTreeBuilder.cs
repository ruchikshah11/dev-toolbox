using System.Xml.Linq;
using DevToolbox.UI;

namespace DevToolbox.Tools.XmlFormatter
{
    // Builds the collapsible TreeView representation of a parsed XML element, mirroring
    // JsonFormatter's JsonTreeBuilder: a node with child elements collapses to a summary count,
    // a leaf node shows its text value inline.
    internal static class XmlTreeBuilder
    {
        public static TreeNode BuildNode(XElement element)
        {
            var attrs = element.Attributes().ToList();
            var children = element.Elements().ToList();

            var tagLabel = "<" + element.Name.LocalName + ">";
            if (attrs.Count > 0)
            {
                tagLabel += "  " + string.Join(" ", attrs.Select(a => $"{a.Name.LocalName}=\"{a.Value}\""));
            }

            string separator;
            string valueText;
            Color valueColor;

            if (children.Count > 0)
            {
                separator = "  ";
                valueText = $"{{ {children.Count} {(children.Count == 1 ? "child element" : "child elements")} }}";
                valueColor = MarkupSyntaxColors.Doctype;
            }
            else
            {
                var text = element.Value.Trim();
                if (text.Length > 0)
                {
                    separator = ": ";
                    valueText = text;
                    valueColor = MarkupSyntaxColors.AttributeValue;
                }
                else
                {
                    separator = "";
                    valueText = "";
                    valueColor = MarkupSyntaxColors.AttributeValue;
                }
            }

            var node = MarkupTreeView.MakeNode(tagLabel, separator, valueText, MarkupSyntaxColors.TagName, valueColor);
            foreach (var child in children)
            {
                node.Nodes.Add(BuildNode(child));
            }

            return node;
        }
    }
}
