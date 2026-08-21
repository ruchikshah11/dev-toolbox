using System.Diagnostics;
using DevToolbox.UI;
using Microsoft.Win32;

namespace DevToolbox.Tools.MarkdownPreviewer
{
    /// <summary>
    /// A live, side-by-side Markdown editor and preview: plain source on the left, a rendered
    /// WebBrowser view on the right, both updating on every keystroke.
    /// </summary>
    public class MarkdownPreviewerControl : UserControl
    {
        private const string InitialMarkdown =
            "# Markdown Previewer\n\nType Markdown on the left, see it rendered on the right.\n\n- Supports **bold**, *italic*, `code`\n- Tables, lists, and links\n";

        private readonly TextBox _editor = new();
        private readonly WebBrowser _browser = new();
        private readonly Button _btnOpenInBrowser = new();
        private readonly Label _lblError = new();

        /// <summary>Ensures modern IE rendering mode, builds the action bar and the editor/preview split view, then renders the starter Markdown.</summary>
        public MarkdownPreviewerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            EnsureModernRenderingMode();

            // Dock=Fill must be added before the Dock=Top bar below it - see the docking order
            // note in MainForm/JsonFormatterControl.
            var card = CardPanel.Add(this, "Markdown Previewer - edit on the left, live preview on the right", 0, fill: true);
            BuildSplitView(card);

            BuildActionBar();

            _editor.Text = InitialMarkdown;
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

        /// <summary>Fills the left split pane with the plain-text Markdown source editor.</summary>
        private void BuildEditorPane(Panel host)
        {
            host.BackColor = Theme.Card;

            _editor.Dock = DockStyle.Fill;
            _editor.Multiline = true;
            _editor.BorderStyle = BorderStyle.None;
            _editor.Font = Theme.MonoFont;
            _editor.BackColor = Theme.Card;
            _editor.ForeColor = Theme.Text;
            _editor.AcceptsTab = true;
            _editor.AcceptsReturn = true;
            _editor.ScrollBars = ScrollBars.Both;
            _editor.WordWrap = false;
            _editor.TextChanged += (_, _) => RenderPreview();
            host.Controls.Add(_editor);
        }

        /// <summary>Fills the right split pane with the rendered-output WebBrowser control.</summary>
        private void BuildPreviewPane(Panel host)
        {
            _browser.Dock = DockStyle.Fill;
            _browser.ScriptErrorsSuppressed = true;
            host.Controls.Add(_browser);
        }

        /// <summary>Converts the editor's current Markdown to HTML and pushes it into the preview browser, or shows the error if conversion fails.</summary>
        private void RenderPreview()
        {
            try
            {
                _browser.DocumentText = MarkdownPreviewerService.ToHtmlDocument(_editor.Text);
                HideError();
            }
            catch (Exception ex)
            {
                ShowError($"Could not render: {ex.Message}");
            }
        }

        /// <summary>Writes the rendered HTML to a temp file and opens it in the user's default browser, for full-fidelity rendering the embedded preview can't provide.</summary>
        private void OpenInBrowser()
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), $"devtoolbox-markdown-{Guid.NewGuid():N}.html");
                File.WriteAllText(path, MarkdownPreviewerService.ToHtmlDocument(_editor.Text));
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                HideError();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                ShowError($"Could not open in browser: {ex.Message}");
            }
        }

        // The WebBrowser control defaults to IE7 "quirks" rendering unless the hosting exe opts
        // into a modern IE mode via this per-user registry value, keyed to the exe's own file
        // name. Without this, the preview would render modern HTML/CSS badly broken - this is
        // the standard fix for every WinForms app using WebBrowser (see HtmlViewerControl).
        private static void EnsureModernRenderingMode()
        {
            try
            {
                const string keyPath = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
                var exeName = Path.GetFileName(Application.ExecutablePath);

                using var key = Registry.CurrentUser.CreateSubKey(keyPath);
                var currentValue = key?.GetValue(exeName) as int?;
                if (currentValue is null || currentValue < 11000)
                {
                    key?.SetValue(exeName, 11001, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                // Best-effort only - if the registry write is blocked (locked-down environment),
                // the preview still works, just in legacy IE7 quirks mode.
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
