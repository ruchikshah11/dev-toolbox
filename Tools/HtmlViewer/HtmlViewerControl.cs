using System.Diagnostics;
using DevToolbox.UI;

namespace DevToolbox.Tools.HtmlViewer
{
    /// <summary>
    /// A live, side-by-side HTML editor and preview: syntax-highlighted, line-numbered source on
    /// the left, a rendered WebBrowser view on the right, both updating on every keystroke.
    /// </summary>
    public class HtmlViewerControl : UserControl
    {
        private readonly CodeEditorBox _editor = new();
        private readonly LineNumberGutter _gutter;
        private readonly WebBrowser _browser = new();
        private readonly Button _btnOpenInBrowser = new();
        private readonly Label _lblError = new();

        /// <summary>Ensures modern IE rendering mode, builds the action bar and the editor/preview split view.</summary>
        public HtmlViewerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            _gutter = new LineNumberGutter(_editor);

            WebBrowserCompat.EnsureModernRenderingMode();

            // Dock=Fill must be added before the Dock=Top bar below it - see the docking order
            // note in MainForm/JsonFormatterControl.
            var card = CardPanel.Add(this, "HTML Viewer - edit on the left, live preview on the right", 0, fill: true);
            BuildSplitView(card);

            BuildActionBar();
        }

        /// <summary>Builds the top action bar: the "Open in Default Browser" button and the (normally hidden) render-error label.</summary>
        private void BuildActionBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 14)
            };
            Controls.Add(bar);

            _btnOpenInBrowser.Text = "Open in Default Browser";
            _btnOpenInBrowser.UseMnemonic = false;
            _btnOpenInBrowser.Location = new Point(18, 4);
            _btnOpenInBrowser.Size = new Size(190, 32);
            Theme.StyleSecondaryButton(_btnOpenInBrowser);
            _btnOpenInBrowser.Click += (_, _) => OpenInBrowser();
            bar.Controls.Add(_btnOpenInBrowser);

            _lblError.Location = new Point(220, 10);
            _lblError.Size = new Size(600, 24);
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);
        }

        /// <summary>Builds the resizable, side-by-side split view and places the editor/preview panes inside it.</summary>
        private void BuildSplitView(Panel card)
        {
            var split = CardPanel.AddSplitView(card);

            BuildEditorPane(split.Panel1);
            BuildPreviewPane(split.Panel2);
        }

        /// <summary>Fills the left split pane with the syntax-highlighted, line-numbered HTML source editor.</summary>
        private void BuildEditorPane(Panel host)
        {
            host.BackColor = Theme.Card;

            _editor.Dock = DockStyle.Fill;
            _editor.BorderStyle = BorderStyle.None;
            _editor.Font = Theme.MonoFont;
            _editor.BackColor = Theme.Card;
            _editor.ForeColor = Theme.Text;
            _editor.AcceptsTab = true;
            _editor.WordWrap = false;
            // Forced, not the auto Both - see the note in TabbedOutputView on why the automatic
            // horizontal scrollbar can't be relied on for WordWrap=false content set via code.
            _editor.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
            _editor.TextChanged += (_, _) => OnEditorTextChanged();
            host.Controls.Add(_editor);

            _gutter.Dock = DockStyle.Left;
            host.Controls.Add(_gutter);
        }

        /// <summary>Fills the right split pane with the rendered-output WebBrowser control.</summary>
        private void BuildPreviewPane(Panel host)
        {
            _browser.Dock = DockStyle.Fill;
            _browser.ScriptErrorsSuppressed = true;
            host.Controls.Add(_browser);
        }

        /// <summary>Re-highlights the editor's syntax and re-renders the preview - called on every keystroke.</summary>
        private void OnEditorTextChanged()
        {
            HighlightSyntax();
            RenderPreview();
        }

        /// <summary>Re-colors the editor's text by token kind (tag/attribute/string/etc.), preserving the caret position and scroll offset across the rebuild - delegates to the shared markup highlighter used by every other tag-markup pane in the app.</summary>
        private void HighlightSyntax() => MarkupHighlighter.Highlight(_editor);

        /// <summary>Pushes the editor's current HTML into the WebBrowser preview.</summary>
        private void RenderPreview()
        {
            try
            {
                _browser.DocumentText = string.IsNullOrEmpty(_editor.Text) ? "<html><body></body></html>" : _editor.Text;
                HideError();
            }
            catch (Exception ex)
            {
                ShowError($"Could not render: {ex.Message}");
            }
        }

        /// <summary>Writes the editor's HTML to a temp file and opens it in the user's default browser, for full-fidelity rendering the embedded preview can't provide.</summary>
        private void OpenInBrowser()
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), $"devtoolbox-preview-{Guid.NewGuid():N}.html");
                File.WriteAllText(path, _editor.Text ?? string.Empty);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                HideError();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                ShowError($"Could not open in browser: {ex.Message}");
            }
        }

        /// <summary>Shows the given message in the error label.</summary>
        private void ShowError(string message)
        {
            _lblError.Text = message;
            _lblError.Visible = true;
        }

        /// <summary>Hides the error label.</summary>
        private void HideError() => _lblError.Visible = false;
    }
}
