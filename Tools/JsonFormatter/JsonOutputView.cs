using System.Linq;
using DevToolbox.UI;
using Newtonsoft.Json.Linq;

namespace DevToolbox.Tools.JsonFormatter
{
    /// <summary>
    /// Displays formatted JSON two ways - as colorized text, and as a collapsible, colorized
    /// tree - so it can be embedded inline (JsonFormatterControl) or hosted full-size in its
    /// own window (ResultWindowForm) without duplicating the rendering logic.
    ///
    /// The text pane is a SyntaxTextPanel (custom-drawn, self-wrapping, AutoScroll-based) rather
    /// than a RichTextBox or WebBrowser - see that class for why: neither RichEdit nor IE11's
    /// engine could be made to wrap and scroll this content predictably, despite extensive
    /// attempts at both.
    /// </summary>
    public class JsonOutputView : UserControl
    {
        private readonly TabControl _tabs = new();
        private readonly TabPage _textPage = new("Formatted Text");
        private SyntaxTextPanel _textPanel = new();
        private readonly TreeView _tree = new();

        // Exposed so a hosting control (JsonFormatterControl's output pane, ResultWindowForm) can
        // wire its own single Copy button rather than this view building a second, redundant one
        // - a caller-provided title bar plus this view's own toolbar previously stacked into an
        // empty-looking double header.
        public string FormattedText => _textPanel.PlainText;

        public JsonOutputView()
        {
            Dock = DockStyle.Fill;

            _tabs.Dock = DockStyle.Fill;
            _tabs.Font = Theme.BaseFont;

            _textPanel.Dock = DockStyle.Fill;
            _textPage.Controls.Add(_textPanel);

            var treePage = new TabPage("Tree View");
            _tree.Dock = DockStyle.Fill;
            _tree.BorderStyle = BorderStyle.None;
            _tree.Font = Theme.MonoFont;
            _tree.BackColor = Theme.Card;
            _tree.FullRowSelect = true;
            _tree.HideSelection = false;
            _tree.DrawMode = TreeViewDrawMode.OwnerDrawText;
            _tree.DrawNode += OnDrawTreeNode;
            treePage.Controls.Add(_tree);

            _tabs.TabPages.Add(_textPage);
            _tabs.TabPages.Add(treePage);

            Controls.Add(_tabs);
        }

        public void Clear()
        {
            _textPanel.Clear();
            _tree.Nodes.Clear();
        }

        public void Render(List<JsonSegment> segments, JToken rootToken)
        {
            RenderText(segments);
            RenderTree(rootToken);
        }

        /// <summary>
        /// Builds a brand new SyntaxTextPanel rather than repainting the same long-lived one -
        /// see TabbedOutputView.SetFormattedText for why: the same instance's on-screen surface
        /// could get stuck out of sync with what it actually computed, confirmed directly via a
        /// DrawToBitmap dump showing correct content while the real window still showed stale text.
        /// </summary>
        private void RenderText(List<JsonSegment> segments)
        {
            var old = _textPanel;

            _textPanel = new SyntaxTextPanel { Dock = DockStyle.Fill };
            _textPage.Controls.Add(_textPanel);
            _textPage.Controls.Remove(old);
            old.Dispose();

            _textPanel.SetContent(segments.Select(s => (s.Text, JsonColors.For(s.Kind))));
        }

        private void RenderTree(JToken rootToken)
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();
            var root = JsonTreeBuilder.BuildNode("(root)", rootToken);
            _tree.Nodes.Add(root);
            root.ExpandAll();
            _tree.EndUpdate();
        }

        private static void OnDrawTreeNode(object? sender, DrawTreeNodeEventArgs e)
        {
            var node = e.Node;
            if (node?.Tag is not JsonTreeBuilder.NodeTagInfo info)
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
            x += DrawSegment(e.Graphics, info.Separator, font, JsonColors.Structural, x, bounds.Top);
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
