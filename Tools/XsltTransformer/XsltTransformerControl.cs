using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.XsltTransformer
{
    /// <summary>
    /// Bespoke control (rather than TextTransformControl) because this tool needs two distinct
    /// text inputs - the XML document and the XSLT stylesheet - plus a single output.
    /// </summary>
    public class XsltTransformerControl : UserControl
    {
        // RichTextBox, not TextBox, so every pane can be colorized as XML/XSLT markup - see the
        // TextChanged wiring in BuildInputCard/BuildOutputCard.
        private readonly RichTextBox _txtXml = new();
        private readonly RichTextBox _txtXslt = new();
        private readonly RichTextBox _output = new();
        private readonly Label _lblError = new();

        // Hint text shown while each box is empty and unfocused - the "Choose File..." button
        // sitting right above an otherwise-blank box reads as "file upload only" without this.
        private RichTextBoxPlaceholder _xmlPlaceholder = null!;
        private RichTextBoxPlaceholder _xsltPlaceholder = null!;

        public XsltTransformerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it, and since same-edge
            // Dock=Top siblings stack in reverse add-order (see TextTransformControl), the
            // action bar is added first (ends up lowest of the Top band) and the XML card is
            // added last (ends up visually topmost) to get: XML card, XSLT card, action bar.
            var outputCard = CardPanel.Add(this, "Transform Output", 0, fill: true);
            BuildOutputCard(outputCard);

            BuildActionBar();
            _xsltPlaceholder = BuildInputCard(_txtXslt, "XSLT Stylesheet", 220, "Choose an XSLT file", "XSLT files (*.xslt;*.xsl)|*.xslt;*.xsl|All files (*.*)|*.*",
                "Paste or type your XSLT stylesheet here, or use Choose File to load one.");
            _xmlPlaceholder = BuildInputCard(_txtXml, "XML Input", 220, "Choose an XML file", "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                "Paste or type your XML document here, or use Choose File to load one.");
        }

        private RichTextBoxPlaceholder BuildInputCard(RichTextBox textBox, string title, int height, string dialogTitle, string filter, string placeholderText)
        {
            var card = CardPanel.Add(this, title, height);

            var btnChooseFile = new Button { Text = "Choose File...", Size = new Size(120, 26) };
            Theme.StyleSecondaryButton(btnChooseFile);
            btnChooseFile.Click += (_, _) => LoadFileInto(textBox, dialogTitle, filter);
            card.Controls.Add(btnChooseFile);
            void PositionButton() => btnChooseFile.Location = new Point(card.Width - 18 - btnChooseFile.Width, 8);
            card.Resize += (_, _) => PositionButton();
            PositionButton();

            textBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            textBox.AcceptsTab = true;
            textBox.TextChanged += (_, _) => MarkupHighlighter.Highlight(textBox);
            CardPanel.WrapWithBorder(card, textBox, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);

            return new RichTextBoxPlaceholder(textBox, placeholderText);
        }

        private void LoadFileInto(RichTextBox target, string dialogTitle, string filter)
        {
            using var dialog = new OpenFileDialog { Title = dialogTitle, Filter = filter };
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

        private void BuildActionBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 14)
            };
            Controls.Add(bar);

            var btnTransform = new Button
            {
                Text = "Transform",
                Location = new Point(18, 8),
                Size = new Size(140, 32)
            };
            Theme.StylePrimaryButton(btnTransform);
            btnTransform.Click += (_, _) => RunTransform();
            bar.Controls.Add(btnTransform);

            _lblError.Location = new Point(18, 46);
            _lblError.Size = new Size(700, 26);
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);
        }

        private void BuildOutputCard(Panel card)
        {
            var btnCopy = new Button { Text = "Copy to Clipboard", Size = new Size(150, 28) };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (_, _) =>
            {
                if (_output.Text.Length > 0) Clipboard.SetText(_output.Text);
            };
            card.Controls.Add(btnCopy);

            void PositionCopy() => btnCopy.Location = new Point(card.Width - 18 - btnCopy.Width, 8);
            card.Resize += (_, _) => PositionCopy();
            PositionCopy();

            _output.ReadOnly = true;
            _output.ScrollBars = RichTextBoxScrollBars.Vertical;
            _output.TextChanged += (_, _) => MarkupHighlighter.Highlight(_output);
            CardPanel.WrapWithBorder(card, _output, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void RunTransform()
        {
            try
            {
                _output.Text = XsltTransformerService.Transform(_xmlPlaceholder.GetText(), _xsltPlaceholder.GetText());
                HideError();
            }
            catch (Exception ex)
            {
                _output.Text = string.Empty;
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
