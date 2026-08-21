using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.XmlValidator
{
    // Bespoke (not TextTransformControl) because this needs two inputs - the XML document and
    // an optional XSD schema - rather than a single paste box. Still uses CardPanel's shared
    // split-view scaffolding for the overall left (inputs) / right (result) layout, so it stays
    // in sync with every other formatter/validator tool built the same way.
    public class XmlValidatorControl : UserControl
    {
        // RichTextBox, not TextBox, so the XML pane can be colorized as markup - the validation
        // result stays a plain TextBox since it's a message, not markup.
        private readonly RichTextBox _txtXml = new();
        private readonly TextBox _txtOutput = new();
        private readonly Button _btnValidate = new();
        private readonly Label _lblError = new();
        private readonly Label _lblXsdStatus = new();

        // Hint text shown while the XML box is empty and unfocused - the "Choose File" button
        // sitting right above an otherwise-blank box reads as "file upload only" without this.
        private RichTextBoxPlaceholder _xmlPlaceholder = null!;

        // The optional XSD schema, loaded via file only (see BuildActionBar) - no dedicated
        // paste box for it, unlike an earlier version of this tool. That second box made the XML
        // Document pane look like it was split into two competing sections instead of one
        // full-height paste target matching JSON/HTML Validator's single-input layout, and a
        // paste meant for the XML document could land in the wrong box.
        private string? _xsdSchemaText;

        /// <summary>
        /// Builds a top action bar (matching XmlFormatterControl/JSON Validator's convention -
        /// Choose File + Validate up top, not buried at the bottom of the input pane) plus the
        /// input/output split view: XML + optional XSD schema on the left, the result on the right.
        /// </summary>
        public XmlValidatorControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top bar below it - see the docking order
            // note in MainForm/JsonFormatterControl.
            var card = CardPanel.Add(this, "XML Validator - XML/XSD on the left, validation result on the right", 0, fill: true);
            var split = CardPanel.AddSplitView(card);

            BuildInputPane(split.Panel1);
            CardPanel.FillSplitPane(split.Panel2, "Validation Result", _txtOutput, onCopy: () =>
            {
                if (_txtOutput.Text.Length > 0) Clipboard.SetText(_txtOutput.Text);
            });

            BuildActionBar();
        }

        /// <summary>Builds the top action bar: Choose File (for the XML document), the optional XSD schema file picker, and the Validate button/error label.</summary>
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

            var btnChooseFile = new Button { Text = "Choose File", Location = new Point(18, 4), Size = new Size(110, 32) };
            Theme.StyleSecondaryButton(btnChooseFile);
            btnChooseFile.Click += (_, _) => LoadFileInto(_txtXml, "Choose an XML file", "XML files (*.xml)|*.xml|All files (*.*)|*.*");
            bar.Controls.Add(btnChooseFile);

            var btnChooseXsd = new Button { Text = "XSD Schema (Optional)", Location = new Point(138, 4), Size = new Size(170, 32) };
            Theme.StyleSecondaryButton(btnChooseXsd);
            btnChooseXsd.Click += (_, _) => LoadXsdFile();
            bar.Controls.Add(btnChooseXsd);

            _lblXsdStatus.Text = "No schema loaded";
            _lblXsdStatus.ForeColor = Theme.TextMuted;
            _lblXsdStatus.Font = Theme.BaseFont;
            _lblXsdStatus.AutoSize = false;
            _lblXsdStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblXsdStatus.Location = new Point(316, 8);
            _lblXsdStatus.Size = new Size(160, 24);
            bar.Controls.Add(_lblXsdStatus);

            _btnValidate.Text = "Validate";
            _btnValidate.Location = new Point(486, 4);
            _btnValidate.Size = new Size(130, 32);
            Theme.StylePrimaryButton(_btnValidate);
            _btnValidate.Click += (_, _) => TryValidate();
            bar.Controls.Add(_btnValidate);

            _lblError.Location = new Point(628, 8);
            _lblError.Size = new Size(Math.Max(60, bar.Width - 638), 24);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);
        }

        /// <summary>Fills the left pane with the XML document, filling the whole pane - no second, competing paste box for the (file-only) optional XSD schema.</summary>
        private void BuildInputPane(Panel host)
        {
            host.BackColor = Theme.Card;

            var lblXmlTitle = new Label
            {
                Text = "XML DOCUMENT",
                AutoSize = false,
                Font = Theme.SectionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Background,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            host.Controls.Add(lblXmlTitle);

            _txtXml.Dock = DockStyle.Fill;
            _txtXml.ScrollBars = RichTextBoxScrollBars.Vertical;
            _txtXml.AcceptsTab = true;
            _txtXml.BorderStyle = BorderStyle.None;
            _txtXml.Font = Theme.MonoFont;
            _txtXml.BackColor = Theme.Card;
            _txtXml.ForeColor = Theme.Text;
            _txtXml.TextChanged += (_, _) => MarkupHighlighter.Highlight(_txtXml);
            host.Controls.Add(_txtXml);
            _xmlPlaceholder = new RichTextBoxPlaceholder(_txtXml, "Paste or type your XML document here, or use Choose File to load one.");
        }

        private void LoadXsdFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose an XSD schema file",
                Filter = "XSD files (*.xsd)|*.xsd|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                _xsdSchemaText = EncodingCatalog.Default.GetString(bytes);
                _lblXsdStatus.Text = Path.GetFileName(dialog.FileName);
                _lblXsdStatus.ForeColor = Theme.Text;
                _lblError.Visible = false;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _lblError.Text = $"Could not read file: {ex.Message}";
                _lblError.Visible = true;
            }
        }

        private void LoadFileInto(RichTextBox target, string dialogTitle, string filter)
        {
            using var dialog = new OpenFileDialog { Title = dialogTitle, Filter = filter };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                target.Text = EncodingCatalog.Default.GetString(bytes);
                _lblError.Visible = false;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _lblError.Text = $"Could not read file: {ex.Message}";
                _lblError.Visible = true;
            }
        }

        private void TryValidate()
        {
            try
            {
                var result = XmlValidatorService.Validate(_xmlPlaceholder.GetText(), _xsdSchemaText ?? "");
                _txtOutput.Text = result;
                _lblError.Visible = false;

                // XmlValidatorService only throws for malformed XML/XSD input - actual schema
                // validation failures come back as text here too (an issue list, not an
                // exception), so success has to be read back out of the result.
                _txtOutput.ForeColor = result.StartsWith("Valid XML", StringComparison.Ordinal) ? Theme.Success : Theme.Error;
                _txtOutput.Font = Theme.BoldFont;
            }
            catch (FormatException ex)
            {
                _txtOutput.Text = string.Empty;
                _lblError.Text = ex.Message;
                _lblError.Visible = true;
            }
        }
    }
}
