using DevToolbox.UI;

namespace DevToolbox.Tools.NumberBaseConverter
{
    public class NumberBaseConverterControl : UserControl
    {
        private readonly TextBox _txtInput = new();
        private readonly ComboBox _cboFromBase = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();

        /// <summary>Builds the number/base-picker input card and the live conversion output card.</summary>
        public NumberBaseConverterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking order
            // note used throughout the tool controls.
            BuildOutputCard();
            BuildInputCard();

            Convert();
        }

        /// <summary>Builds the number textbox and "From Base" dropdown, both wired to auto-reconvert on change.</summary>
        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "NUMBER & SOURCE BASE", 150);
            const int labelY = 44, fieldY = 64;

            var lblNumber = CardPanel.AddFieldLabel(card, "Number", 18, labelY);
            _txtInput.Font = Theme.MonoFont;
            _txtInput.Text = "255";
            _txtInput.TextChanged += (_, _) => Convert();
            var numberWrapper = CardPanel.WrapWithBorder(card, _txtInput, new Point(18, fieldY), 300, 30,
                AnchorStyles.Top | AnchorStyles.Left);

            var lblFromBase = CardPanel.AddFieldLabel(card, "From Base", 0, labelY);
            lblFromBase.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cboFromBase.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboFromBase.Font = Theme.BaseFont;
            _cboFromBase.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            foreach (var b in NumberBaseConverterService.Bases) _cboFromBase.Items.Add(b);
            _cboFromBase.SelectedIndex = 2; // Decimal
            _cboFromBase.SelectedIndexChanged += (_, _) => Convert();
            card.Controls.Add(_cboFromBase);

            void PositionPairedFields()
            {
                var comboWidth = 220;
                var numberWidth = card.Width - 36 - comboWidth - 24;
                numberWrapper.Width = Math.Max(120, numberWidth);

                _cboFromBase.Width = comboWidth;
                _cboFromBase.Location = new Point(card.Width - 18 - comboWidth, fieldY);
                lblFromBase.Location = new Point(card.Width - 18 - lblFromBase.Width, labelY);
            }
            card.Resize += (_, _) => PositionPairedFields();
            PositionPairedFields();

            _lblError.Location = new Point(18, 108);
            _lblError.Size = new Size(card.Width - 36, 30);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        /// <summary>Builds the read-only multi-base output card with its Copy button.</summary>
        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "RESULT", 0, fill: true);

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

        /// <summary>Converts the current input against the selected source base and shows all four forms, or the error.</summary>
        private void Convert()
        {
            try
            {
                var fromBase = _cboFromBase.SelectedItem as string ?? NumberBaseConverterService.Bases[2];
                var result = NumberBaseConverterService.Convert(_txtInput.Text, fromBase);
                _txtOutput.Text = string.Join("\r\n", new[]
                {
                    $"Binary: {result.Binary}",
                    $"Octal: {result.Octal}",
                    $"Decimal: {result.Decimal}",
                    $"Hexadecimal: {result.Hexadecimal}"
                });
                HideError();
            }
            catch (Exception ex)
            {
                _txtOutput.Text = string.Empty;
                ShowError(ex.Message);
            }
        }

        /// <summary>Shows the error label with the given message.</summary>
        private void ShowError(string message)
        {
            _lblError.Text = message;
            _lblError.Visible = true;
        }

        /// <summary>Hides the error label.</summary>
        private void HideError() => _lblError.Visible = false;
    }
}
