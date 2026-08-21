using System.Linq;

namespace DevToolbox.UI
{
    /// <summary>
    /// Shared "Formatted Text" + "Tree View" tabbed output, mirroring JsonFormatter's
    /// JsonOutputView but generic - a colorized read-only text pane on one tab, a colorized
    /// collapsible tree on the other. Concrete subclasses (XmlOutputView, HtmlOutputView, ...)
    /// only need to supply their own tree-building logic; the tab scaffolding, text-pane theming
    /// and tree owner-draw painting all live here once.
    ///
    /// The text pane is a SyntaxTextPanel (custom-drawn, self-wrapping, AutoScroll-based) rather
    /// than a RichTextBox or WebBrowser - see that class for why: neither RichEdit nor IE11's
    /// engine could be made to wrap and scroll this content predictably, despite extensive
    /// attempts at both.
    /// </summary>
    internal class TabbedOutputView : UserControl
    {
        private readonly TabPage _textPage = new("Formatted Text");
        private SyntaxTextPanel _textPanel = new();
        protected readonly TreeView Tree = new();

        public string FormattedText => _textPanel.PlainText;

        protected TabbedOutputView()
        {
            Dock = DockStyle.Fill;

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.BaseFont };

            _textPanel.Dock = DockStyle.Fill;
            _textPage.Controls.Add(_textPanel);

            var treePage = new TabPage("Tree View");
            Tree.Dock = DockStyle.Fill;
            MarkupTreeView.Configure(Tree);
            treePage.Controls.Add(Tree);

            tabs.TabPages.Add(_textPage);
            tabs.TabPages.Add(treePage);
            Controls.Add(tabs);
        }

        public void Clear()
        {
            _textPanel.Clear();
            Tree.Nodes.Clear();
        }

        /// <summary>
        /// Replaces the Formatted Text tab's content, colorized as tag markup. Builds a brand new
        /// SyntaxTextPanel rather than repainting the same long-lived one: confirmed directly (a
        /// DrawToBitmap dump of the existing panel showed the wrap/paint logic was already 100%
        /// correct, while the actual on-screen window still showed stale/truncated content even
        /// after Invalidate()+Update()) that the same instance's on-screen surface can get stuck
        /// out of sync with what it actually computed. A fresh control's first paint doesn't carry
        /// forward whatever state causes that.
        /// </summary>
        protected void SetFormattedText(string text)
        {
            var old = _textPanel;

            _textPanel = new SyntaxTextPanel { Dock = DockStyle.Fill };
            _textPage.Controls.Add(_textPanel);
            _textPage.Controls.Remove(old);
            old.Dispose();

            var segments = MarkupSyntaxTokenizer.Tokenize(text)
                .Select(s => (s.Text, MarkupSyntaxColors.For(s.Kind)));
            _textPanel.SetContent(segments);
        }

        /// <summary>Replaces the Tree View tab's roots (built by a subclass's own tree builder) and expands them.</summary>
        protected void SetTreeRoots(IEnumerable<TreeNode> roots)
        {
            Tree.BeginUpdate();
            Tree.Nodes.Clear();
            foreach (var node in roots)
            {
                Tree.Nodes.Add(node);
                node.ExpandAll();
            }
            Tree.EndUpdate();
        }
    }
}
