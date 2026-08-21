using System.Text;
using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.JsonFormatter
{
    /// <summary>
    /// A live, side-by-side JSON formatter: your pasted/uploaded input on the left, the
    /// formatted (or tree-view) output on the right - matching the same split-view convention
    /// used by the HTML Viewer and Markdown Previewer, rather than stacking input above output.
    /// </summary>
    public class JsonFormatterControl : UserControl
    {
        // RichTextBox, not TextBox, so the input can be colorized the same way the output is -
        // see HighlightInput().
        private readonly RichTextBox _txtInput = new();
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly ComboBox _cboEncoding = new();
        private readonly ComboBox _cboIndent = new();
        private readonly ComboBox _cboBracket = new();
        private readonly Button _btnFormat = new();
        private readonly Button _btnFormatNewWindow = new();
        private readonly Label _lblError = new();
        private readonly JsonOutputView _output = new();

        private string? _uploadedFilePath;

        /// <summary>Builds the top action bar (upload/encoding/format options) and the input/output split view below it.</summary>
        public JsonFormatterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top bar below it - see the docking order
            // note in MainForm/HtmlViewerControl.
            var card = CardPanel.Add(this, "JSON Formatter - your input on the left, formatted output on the right", 0, fill: true);
            BuildSplitView(card);

            BuildActionBar();
        }

        /// <summary>Builds the compact, two-row action bar: file upload + encoding on row 1, indent/bracket options + Format buttons + error on row 2.</summary>
        private void BuildActionBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 14)
            };
            Controls.Add(bar);

            // Row 1: file upload + encoding.
            _btnChooseFile.Text = "Choose File";
            _btnChooseFile.Location = new Point(18, 4);
            _btnChooseFile.Size = new Size(110, 30);
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += OnChooseFileClick;
            bar.Controls.Add(_btnChooseFile);

            _lblFileName.Text = "No file chosen";
            _lblFileName.ForeColor = Theme.TextMuted;
            _lblFileName.Font = Theme.BaseFont;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Location = new Point(136, 4);
            _lblFileName.Size = new Size(160, 30);
            bar.Controls.Add(_lblFileName);

            AddInlineLabel(bar, "Encoding:", 306, 12);
            _cboEncoding.Location = new Point(374, 6);
            _cboEncoding.Width = 180;
            _cboEncoding.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboEncoding.Font = Theme.BaseFont;
            foreach (var (display, encoding) in EncodingCatalog.Available)
            {
                _cboEncoding.Items.Add(new EncodingItem(display, encoding));
            }
            if (_cboEncoding.Items.Count > 0) _cboEncoding.SelectedIndex = 0;
            _cboEncoding.SelectedIndexChanged += OnEncodingChanged;
            bar.Controls.Add(_cboEncoding);

            // Row 2: indentation + bracket style + format buttons + error, all on one line.
            AddInlineLabel(bar, "Indent:", 18, 50);
            _cboIndent.Location = new Point(70, 44);
            _cboIndent.Width = 190;
            _cboIndent.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboIndent.Font = Theme.BaseFont;
            foreach (var option in IndentOption.All) _cboIndent.Items.Add(option);
            _cboIndent.SelectedIndex = 0;
            bar.Controls.Add(_cboIndent);

            AddInlineLabel(bar, "Brackets:", 272, 50);
            _cboBracket.Location = new Point(340, 44);
            _cboBracket.Width = 200;
            _cboBracket.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboBracket.Font = Theme.BaseFont;
            foreach (var option in BracketOption.All) _cboBracket.Items.Add(option);
            _cboBracket.SelectedIndex = 0;
            bar.Controls.Add(_cboBracket);

            _btnFormat.Text = "Format JSON";
            _btnFormat.Location = new Point(552, 44);
            _btnFormat.Size = new Size(120, 30);
            Theme.StylePrimaryButton(_btnFormat);
            _btnFormat.Click += (_, _) => TryFormat(openInNewWindow: false);
            bar.Controls.Add(_btnFormat);

            _btnFormatNewWindow.Text = "New Window";
            _btnFormatNewWindow.Location = new Point(680, 44);
            _btnFormatNewWindow.Size = new Size(110, 30);
            Theme.StyleSecondaryButton(_btnFormatNewWindow);
            _btnFormatNewWindow.Click += (_, _) => TryFormat(openInNewWindow: true);
            bar.Controls.Add(_btnFormatNewWindow);

            _lblError.Location = new Point(800, 48);
            _lblError.Size = new Size(Math.Max(60, bar.Width - 18 - 800), 24);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);
        }

        /// <summary>Adds a small bold muted label at the given position - a compact, inline-left alternative to CardPanel.AddFieldLabel's stacked-above style, used to pack more fields per row.</summary>
        private static Label AddInlineLabel(Control parent, string text, int x, int y)
        {
            var label = new Label
            {
                Text = text,
                Font = Theme.BoldFont,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(x, y)
            };
            parent.Controls.Add(label);
            return label;
        }

        /// <summary>Builds the resizable split view: your input on the left, the formatted/tree output on the right - using CardPanel's shared split-view scaffolding, so it stays in sync with TextTransformControl and any other tool built the same way.</summary>
        private void BuildSplitView(Panel card)
        {
            var split = CardPanel.AddSplitView(card);

            _txtInput.ScrollBars = RichTextBoxScrollBars.Vertical;
            _txtInput.AcceptsTab = true;
            _txtInput.TextChanged += (_, _) => HighlightInput();
            CardPanel.FillSplitPane(split.Panel1, "Your Input", _txtInput);

            // JsonOutputView (unlike TextTransformControl's plain output textbox) exposes
            // FormattedText specifically so this Copy button doesn't need its own reference to
            // the underlying RichTextBox - see the note on that property.
            CardPanel.FillSplitPane(split.Panel2, "Formatted Output", _output, onCopy: () =>
            {
                if (_output.FormattedText.Length > 0) Clipboard.SetText(_output.FormattedText);
            });
        }

        /// <summary>Re-colors the input by token kind (key/string/number/.../structural) - the same palette the formatted output uses - preserving the caret and scroll position across the rebuild. Runs on every keystroke, so invalid/incomplete JSON is tolerated rather than left throwing. Delegates to the shared JsonHighlighter so any other JSON-aware input (e.g. the JSON Validator) recolors the same way.</summary>
        private void HighlightInput() => JsonHighlighter.Highlight(_txtInput);

        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose a JSON file",
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _uploadedFilePath = dialog.FileName;
            _lblFileName.Text = Path.GetFileName(_uploadedFilePath);
            LoadUploadedFile();
        }

        private void OnEncodingChanged(object? sender, EventArgs e)
        {
            if (_uploadedFilePath is not null) LoadUploadedFile();
        }

        private void LoadUploadedFile()
        {
            try
            {
                var bytes = File.ReadAllBytes(_uploadedFilePath!);
                var encoding = (_cboEncoding.SelectedItem as EncodingItem)?.Encoding ?? EncodingCatalog.Default;
                _txtInput.Text = encoding.GetString(bytes);
                HideError();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowError($"Could not read file: {ex.Message}");
            }
        }

        private void TryFormat(bool openInNewWindow)
        {
            try
            {
                var token = JsonFormatterService.Parse(_txtInput.Text);
                var indent = ((IndentOption)_cboIndent.SelectedItem!).Style;
                var bracket = ((BracketOption)_cboBracket.SelectedItem!).Style;

                // Compact/JS-escaped are inherently comment-free (that's what makes them valid,
                // minified JSON); the indented styles preserve any "//" or "/* */" comments the
                // user wrote, by re-lexing the raw text instead of walking the parsed JToken.
                var segments = indent is JsonIndentStyle.Compact or JsonIndentStyle.JavaScriptEscaped
                    ? JsonFormatterService.FormatSegments(token, indent, bracket)
                    : JsonFormatterService.FormatSegmentsPreservingComments(_txtInput.Text, indent, bracket);
                HideError();

                if (openInNewWindow)
                {
                    new ResultWindowForm("Formatted JSON", segments, token).Show();
                }
                else
                {
                    _output.Render(segments, token);
                }
            }
            catch (FormatException ex)
            {
                _output.Clear();
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            _lblError.Text = message;
            _lblError.Visible = true;
        }

        private void HideError() => _lblError.Visible = false;

        private sealed class EncodingItem
        {
            public EncodingItem(string display, Encoding encoding)
            {
                Display = display;
                Encoding = encoding;
            }

            public string Display { get; }
            public Encoding Encoding { get; }
            public override string ToString() => Display;
        }

        private sealed class IndentOption
        {
            public static readonly IndentOption[] All =
            {
                new("2 spaces per indent level", JsonIndentStyle.TwoSpaces),
                new("3 spaces per indent level", JsonIndentStyle.ThreeSpaces),
                new("4 spaces per indent level", JsonIndentStyle.FourSpaces),
                new("Tab delimited", JsonIndentStyle.Tab),
                new("Compact (1 line)", JsonIndentStyle.Compact),
                new("JavaScript escaped", JsonIndentStyle.JavaScriptEscaped),
            };

            private IndentOption(string display, JsonIndentStyle style)
            {
                Display = display;
                Style = style;
            }

            public string Display { get; }
            public JsonIndentStyle Style { get; }
            public override string ToString() => Display;
        }

        private sealed class BracketOption
        {
            public static readonly BracketOption[] All =
            {
                new("Collapsed (braces on same line)", JsonBracketStyle.Collapsed),
                new("Expanded (braces on new line)", JsonBracketStyle.Expanded),
            };

            private BracketOption(string display, JsonBracketStyle style)
            {
                Display = display;
                Style = style;
            }

            public string Display { get; }
            public JsonBracketStyle Style { get; }
            public override string ToString() => Display;
        }
    }
}
