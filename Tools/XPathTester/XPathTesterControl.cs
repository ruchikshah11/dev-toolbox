using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.XPathTester
{
    // Bespoke (not TextTransformControl) because this needs an XML document plus a separate
    // XPath expression row, not a single paste box. Still uses CardPanel's shared split-view
    // scaffolding for the overall left (XML + expression) / right (matches) layout.
    public class XPathTesterControl : UserControl
    {
        // RichTextBox, not TextBox, so the XML input and the matching-nodes output can both be
        // colorized as markup.
        private readonly RichTextBox _txtXml = new();
        private readonly TextBox _txtXPath = new();
        private readonly RichTextBox _txtOutput = new();
        private readonly Button _btnEvaluate = new();
        private readonly Label _lblError = new();

        // Hint text shown while the XML box is empty and unfocused - the "Choose File..." button
        // sitting right above an otherwise-blank box reads as "file upload only" without this.
        private RichTextBoxPlaceholder _xmlPlaceholder = null!;

        /// <summary>
        /// Builds a top action bar (matching XmlFormatterControl/JSON Validator's convention -
        /// Choose File and the expression/Evaluate row up top, not buried at the bottom of the
        /// input pane) plus the input/output split view: the XML document on the left, matching
        /// nodes on the right.
        /// </summary>
        public XPathTesterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top bar below it - see the docking order
            // note in MainForm/JsonFormatterControl.
            var card = CardPanel.Add(this, "XPath Tester - XML on the left, matches on the right", 0, fill: true);
            var split = CardPanel.AddSplitView(card);

            BuildInputPane(split.Panel1);
            _txtOutput.ReadOnly = true;
            _txtOutput.TextChanged += (_, _) => MarkupHighlighter.Highlight(_txtOutput);
            CardPanel.FillSplitPane(split.Panel2, "Matching Nodes", _txtOutput, onCopy: () =>
            {
                if (_txtOutput.Text.Length > 0) Clipboard.SetText(_txtOutput.Text);
            });

            BuildActionBar();
        }

        /// <summary>Builds the top action bar: Choose File (for the XML document) on row 1, the XPath expression field + Evaluate button + error label on row 2.</summary>
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

            var btnChooseFile = new Button { Text = "Choose File", Location = new Point(18, 4), Size = new Size(110, 32) };
            Theme.StyleSecondaryButton(btnChooseFile);
            btnChooseFile.Click += (_, _) => LoadFileInto(_txtXml);
            bar.Controls.Add(btnChooseFile);

            var lblExpression = new Label
            {
                Text = "XPath Expression:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(18, 50)
            };
            bar.Controls.Add(lblExpression);

            _txtXPath.Font = Theme.MonoFont;
            _txtXPath.Location = new Point(148, 46);
            _txtXPath.Size = new Size(300, 24);
            _txtXPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            bar.Controls.Add(_txtXPath);

            _btnEvaluate.Text = "Evaluate";
            _btnEvaluate.Location = new Point(458, 44);
            _btnEvaluate.Size = new Size(110, 30);
            _btnEvaluate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StylePrimaryButton(_btnEvaluate);
            _btnEvaluate.Click += (_, _) => Evaluate();
            bar.Controls.Add(_btnEvaluate);

            _lblError.Location = new Point(18, 8);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);

            void PositionBar()
            {
                _txtXPath.Width = Math.Max(60, bar.Width - 18 - 458);
                _btnEvaluate.Location = new Point(bar.Width - 18 - _btnEvaluate.Width, 44);
                _lblError.Size = new Size(Math.Max(60, bar.Width - 36), 24);
            }
            bar.Resize += (_, _) => PositionBar();
            PositionBar();
        }

        /// <summary>Fills the left pane with the XML document, filling the whole pane.</summary>
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

        private void LoadFileInto(RichTextBox target)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose an XML file",
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                target.Text = EncodingCatalog.Default.GetString(bytes);
                HideError();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowError($"Could not read file: {ex.Message}");
            }
        }

        private void Evaluate()
        {
            try
            {
                _txtOutput.Text = XPathTesterService.Evaluate(_xmlPlaceholder.GetText(), _txtXPath.Text);
                HideError();
            }
            catch (Exception ex)
            {
                _txtOutput.Text = string.Empty;
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            _lblError.Text = message;
            _lblError.Visible = true;
        }

        private void HideError() => _lblError.Visible = false;
    }
}
