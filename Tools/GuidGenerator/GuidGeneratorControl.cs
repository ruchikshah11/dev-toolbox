using DevToolbox.UI;

namespace DevToolbox.Tools.GuidGenerator
{
    public class GuidGeneratorControl : UserControl
    {
        private readonly NumericUpDown _numQuantity = new();
        private readonly CheckBox _chkUppercase = new();
        private readonly CheckBox _chkHyphens = new();
        private readonly CheckBox _chkBraces = new();
        private readonly Button _btnGenerate = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();

        public GuidGeneratorControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking
            // order note in MainForm/JsonFormatterControl.
            BuildOutputCard();
            BuildOptionsCard();

            Generate();
        }

        private void BuildOptionsCard()
        {
            var card = CardPanel.Add(this, "OPTIONS", 190);

            CardPanel.AddFieldLabel(card, "Quantity", 18, 44);
            _numQuantity.Location = new Point(18, 64);
            _numQuantity.Width = 100;
            _numQuantity.Minimum = 1;
            _numQuantity.Maximum = 1000;
            _numQuantity.Value = 1;
            _numQuantity.Font = Theme.BaseFont;
            card.Controls.Add(_numQuantity);

            _chkHyphens.Text = "Include hyphens";
            _chkHyphens.Checked = true;
            _chkHyphens.Font = Theme.BaseFont;
            _chkHyphens.ForeColor = Theme.Text;
            _chkHyphens.AutoSize = true;
            _chkHyphens.Location = new Point(140, 66);
            card.Controls.Add(_chkHyphens);

            _chkUppercase.Text = "Uppercase";
            _chkUppercase.Font = Theme.BaseFont;
            _chkUppercase.ForeColor = Theme.Text;
            _chkUppercase.AutoSize = true;
            _chkUppercase.Location = new Point(290, 66);
            card.Controls.Add(_chkUppercase);

            _chkBraces.Text = "Wrap in braces { }";
            _chkBraces.Font = Theme.BaseFont;
            _chkBraces.ForeColor = Theme.Text;
            _chkBraces.AutoSize = true;
            _chkBraces.Location = new Point(410, 66);
            card.Controls.Add(_chkBraces);

            _btnGenerate.Text = "Generate";
            _btnGenerate.Location = new Point(18, 104);
            _btnGenerate.Size = new Size(140, 32);
            Theme.StylePrimaryButton(_btnGenerate);
            _btnGenerate.Click += (_, _) => Generate();
            card.Controls.Add(_btnGenerate);

            _lblError.Location = new Point(18, 146);
            _lblError.Size = new Size(card.Width - 36, 34);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "GENERATED GUIDS", 0, fill: true);

            var btnCopy = new Button { Text = "Copy to Clipboard", Size = new Size(150, 28) };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (_, _) =>
            {
                if (_txtOutput.Text.Length > 0) Clipboard.SetText(_txtOutput.Text);
            };
            card.Controls.Add(btnCopy);

            void PositionCopy() => btnCopy.Location = new Point(card.Width - 18 - btnCopy.Width, 8);
            card.Resize += (_, _) => PositionCopy();
            PositionCopy();

            _txtOutput.Multiline = true;
            _txtOutput.ReadOnly = true;
            _txtOutput.ScrollBars = ScrollBars.Vertical;
            _txtOutput.Font = Theme.MonoFont;
            CardPanel.WrapWithBorder(card, _txtOutput, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void Generate()
        {
            try
            {
                _txtOutput.Text = GuidGeneratorService.Generate(
                    (int)_numQuantity.Value, _chkUppercase.Checked, _chkHyphens.Checked, _chkBraces.Checked);
                _lblError.Visible = false;
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
