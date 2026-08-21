namespace DevToolbox.UI
{
    /// <summary>
    /// Shared "colorized, collapsible tree" building block for Tree View tabs (XML element tree,
    /// HTML DOM tree, ...): every node paints as a "KeyText" + "Separator" + "ValueText" run in
    /// two colors, exactly like JsonFormatter's JsonTreeBuilder/JsonOutputView pair - kept here so
    /// a new tree-shaped output only needs to build TreeNodes with this Tag, not reimplement the
    /// owner-draw painting.
    /// </summary>
    internal static class MarkupTreeView
    {
        public readonly record struct NodeTagInfo(string KeyText, string Separator, string ValueText, Color KeyColor, Color ValueColor);

        public static TreeNode MakeNode(string key, string separator, string value, Color keyColor, Color valueColor) =>
            new(key + separator + value)
            {
                Tag = new NodeTagInfo(key, separator, value, keyColor, valueColor)
            };

        /// <summary>Applies the shared look (mono font, themed colors, owner-drawn painting) to a TreeView hosting nodes built by MakeNode.</summary>
        public static void Configure(TreeView tree)
        {
            tree.BorderStyle = BorderStyle.None;
            tree.Font = Theme.MonoFont;
            tree.BackColor = Theme.Card;
            tree.FullRowSelect = true;
            tree.HideSelection = false;
            tree.DrawMode = TreeViewDrawMode.OwnerDrawText;
            tree.DrawNode += OnDrawTreeNode;
        }

        private static void OnDrawTreeNode(object? sender, DrawTreeNodeEventArgs e)
        {
            var node = e.Node;
            if (node?.Tag is not NodeTagInfo info)
            {
                e.DrawDefault = true;
                return;
            }

            var bounds = e.Bounds;
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                using var selBrush = new SolidBrush(Theme.AccentSoft);
                e.Graphics.FillRectangle(selBrush, bounds);
            }
            else
            {
                using var bgBrush = new SolidBrush(Theme.Card);
                e.Graphics.FillRectangle(bgBrush, bounds);
            }

            var font = e.Node!.TreeView!.Font;
            var x = bounds.Left;

            x += DrawSegment(e.Graphics, info.KeyText, font, info.KeyColor, x, bounds.Top);
            x += DrawSegment(e.Graphics, info.Separator, font, MarkupSyntaxColors.TagBracket, x, bounds.Top);
            DrawSegment(e.Graphics, info.ValueText, font, info.ValueColor, x, bounds.Top);

            e.DrawDefault = false;
        }

        private static int DrawSegment(Graphics g, string text, Font font, Color color, int x, int y)
        {
            TextRenderer.DrawText(g, text, font, new Point(x, y), color, TextFormatFlags.NoPadding);
            return TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
        }
    }
}
