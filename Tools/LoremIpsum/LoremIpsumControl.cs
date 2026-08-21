using DevToolbox.UI;

namespace DevToolbox.Tools.LoremIpsum
{
    public class LoremIpsumControl : UserControl
    {
        private readonly NumericUpDown _numParagraphs = new();
        private readonly CheckBox _chkTraditionalOpening = new();
        private readonly Button _btnGenerate = new();
        private readonly TextBox _output = new();

        public LoremIpsumControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking order
            // note in TextTransformControl/MainForm.
            var outputCard = CardPanel.Add(this, "Generated Text", 0, fill: true);
            BuildOutputCard(outputCard);

            BuildOptionsCard();

            GenerateText();
        }

        private void BuildOptionsCard()
        {
            var card = CardPanel.Add(this, "OPTIONS", 150);

            CardPanel.AddFieldLabel(card, "Number of paragraphs", 18, 44);
            _numParagraphs.Location = new Point(18, 64);
            _numParagraphs.Width = 100;
            _numParagraphs.Minimum = 1;
            _numParagraphs.Maximum = 50;
            _numParagraphs.Value = 3;
            _numParagraphs.Font = Theme.BaseFont;
            card.Controls.Add(_numParagraphs);

            _chkTraditionalOpening.Text = "Start with 'Lorem ipsum dolor sit amet...'";
            _chkTraditionalOpening.AutoSize = true;
            _chkTraditionalOpening.Checked = true;
            _chkTraditionalOpening.Font = Theme.BaseFont;
            _chkTraditionalOpening.ForeColor = Theme.Text;
            _chkTraditionalOpening.Location = new Point(140, 66);
            card.Controls.Add(_chkTraditionalOpening);

            _btnGenerate.Text = "Generate";
            _btnGenerate.Location = new Point(18, 104);
            _btnGenerate.Size = new Size(120, 32);
            Theme.StylePrimaryButton(_btnGenerate);
            _btnGenerate.Click += (_, _) => GenerateText();
            card.Controls.Add(_btnGenerate);
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

            _output.Multiline = true;
            _output.ReadOnly = true;
            _output.ScrollBars = ScrollBars.Vertical;
            _output.Font = Theme.MonoFont;
            CardPanel.WrapWithBorder(card, _output, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void GenerateText()
        {
            var count = (int)_numParagraphs.Value;
            _output.Text = LoremIpsumService.Generate(count, _chkTraditionalOpening.Checked);
        }
    }
}
