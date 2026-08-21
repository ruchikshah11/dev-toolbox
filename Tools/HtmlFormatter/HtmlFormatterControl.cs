using System.Text;
using DevToolbox.Core;
using DevToolbox.UI;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace DevToolbox.Tools.HtmlFormatter
{
    /// <summary>
    /// A side-by-side HTML formatter: your pasted/uploaded input on the left, the formatted
    /// output on the right - matching the same split-view convention used by JsonFormatterControl
    /// and TextTransformControl (via CardPanel's shared scaffolding).
    /// </summary>
    public class HtmlFormatterControl : UserControl
    {
        // RichTextBox, not TextBox, so the input can be colorized as HTML markup - see the
        // TextChanged wiring in BuildSplitView.
        private readonly RichTextBox _txtInput = new();
        private readonly HtmlOutputView _output = new();
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly ComboBox _cboEncoding = new();
        private readonly ComboBox _cboIndent = new();
        private readonly Button _btnFormat = new();
        private readonly Label _lblError = new();

        private string? _uploadedFilePath;

        /// <summary>Builds the top action bar (upload/encoding/format options) and the input/output split view below it.</summary>
        public HtmlFormatterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top bar below it - see the docking order
            // note in MainForm/JsonFormatterControl.
            var card = CardPanel.Add(this, "HTML Formatter - your input on the left, formatted output on the right", 0, fill: true);
            BuildSplitView(card);

            BuildActionBar();
        }

        /// <summary>Builds the compact, two-row action bar: file upload + encoding on row 1, indentation + Format button + error on row 2.</summary>
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

            AddInlineLabel(bar, "Indent:", 18, 50);
            _cboIndent.Location = new Point(70, 44);
            _cboIndent.Width = 220;
            _cboIndent.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboIndent.Font = Theme.BaseFont;
            foreach (var option in IndentOption.All) _cboIndent.Items.Add(option);
            _cboIndent.SelectedIndex = 0;
            bar.Controls.Add(_cboIndent);

            _btnFormat.Text = "Format HTML";
            _btnFormat.Location = new Point(310, 44);
            _btnFormat.Size = new Size(130, 30);
            Theme.StylePrimaryButton(_btnFormat);
            _btnFormat.Click += (_, _) => TryFormat();
            bar.Controls.Add(_btnFormat);

            _lblError.Location = new Point(452, 48);
            _lblError.Size = new Size(Math.Max(60, bar.Width - 18 - 452), 24);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);
        }

        /// <summary>Adds a small bold muted label at the given position - a compact, inline-left field label used to pack more controls per row.</summary>
        private static void AddInlineLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = Theme.BoldFont,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(x, y)
            });
        }

        /// <summary>Builds the resizable split view: your input on the left, the formatted output on the right.</summary>
        private void BuildSplitView(Panel card)
        {
            var split = CardPanel.AddSplitView(card);

            _txtInput.ScrollBars = RichTextBoxScrollBars.Vertical;
            _txtInput.AcceptsTab = true;
            _txtInput.TextChanged += (_, _) => MarkupHighlighter.Highlight(_txtInput);
            CardPanel.FillSplitPane(split.Panel1, "Your Input", _txtInput);

            CardPanel.FillSplitPane(split.Panel2, "Formatted Output", _output, onCopy: () =>
            {
                if (_output.FormattedText.Length > 0) Clipboard.SetText(_output.FormattedText);
            });
        }

        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose an HTML file",
                Filter = "HTML files (*.html;*.htm)|*.html;*.htm|Text files (*.txt)|*.txt|All files (*.*)|*.*"
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

        private void TryFormat()
        {
            try
            {
                var indentStyle = ((IndentOption)_cboIndent.SelectedItem!).Style;
                var formatted = HtmlFormatterService.Format(_txtInput.Text, indentStyle);

                // Reparses the same text HtmlFormatterService just validated - cheap for the sizes
                // this tool handles, and keeps the Tree View tab in sync without threading the
                // parsed HtmlDocument through the service's return type.
                var doc = new HtmlDocument { OptionOutputAsXml = false };
                doc.LoadHtml(_txtInput.Text);
                _output.Render(formatted, doc.DocumentNode);
                HideError();
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

        private sealed class IndentOption
        {
            public static readonly IndentOption[] All =
            {
                new("2 spaces per indent level", HtmlIndentStyle.TwoSpaces),
                new("3 spaces per indent level", HtmlIndentStyle.ThreeSpaces),
                new("4 spaces per indent level", HtmlIndentStyle.FourSpaces),
                new("Tab delimited", HtmlIndentStyle.Tab),
                new("Compact (1 line)", HtmlIndentStyle.Compact),
            };

            private IndentOption(string display, HtmlIndentStyle style)
            {
                Display = display;
                Style = style;
            }

            public string Display { get; }
            public HtmlIndentStyle Style { get; }
            public override string ToString() => Display;
        }
    }
}
