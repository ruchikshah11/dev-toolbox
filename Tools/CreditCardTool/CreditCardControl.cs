using System.Text;
using DevToolbox.UI;

namespace DevToolbox.Tools.CreditCardTool
{
    public class CreditCardControl : UserControl
    {
        private readonly TextBox _txtCardNumber = new();
        private readonly Button _btnValidate = new();
        private readonly Label _lblValidateResult = new();

        private readonly ComboBox _cboBrand = new();
        private readonly NumericUpDown _numQuantity = new();
        private readonly Button _btnGenerate = new();
        private readonly TextBox _txtGenerated = new();

        public CreditCardControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking
            // order note in MainForm/JsonFormatterControl.
            var generatorCard = CardPanel.Add(this, "Generator (Test Numbers Only)", 0, fill: true);
            BuildGeneratorCard(generatorCard);

            BuildValidatorCard();
        }

        private void BuildValidatorCard()
        {
            var card = CardPanel.Add(this, "Validator (Luhn Checksum)", 150);

            CardPanel.AddFieldLabel(card, "Card number", 18, 44);
            _txtCardNumber.Font = Theme.MonoFont;
            _txtCardNumber.Location = new Point(18, 64);
            _txtCardNumber.Width = 280;
            card.Controls.Add(_txtCardNumber);

            _btnValidate.Text = "Validate (Luhn)";
            _btnValidate.Location = new Point(310, 62);
            _btnValidate.Size = new Size(150, 30);
            Theme.StylePrimaryButton(_btnValidate);
            _btnValidate.Click += (_, _) => ValidateCardNumber();
            card.Controls.Add(_btnValidate);

            _lblValidateResult.Location = new Point(18, 102);
            _lblValidateResult.Size = new Size(card.Width - 36, 30);
            _lblValidateResult.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblValidateResult.Font = Theme.BoldFont;
            _lblValidateResult.AutoEllipsis = true;
            _lblValidateResult.UseMnemonic = false;
            card.Controls.Add(_lblValidateResult);
        }

        private void BuildGeneratorCard(Panel card)
        {
            var lblNote = new Label
            {
                Text = "Generates syntactically valid TEST/FAKE numbers for development use only - these are " +
                       "not real, usable card numbers.",
                ForeColor = Theme.TextMuted,
                Font = Theme.BaseFont,
                Location = new Point(18, 42),
                Size = new Size(card.Width - 36, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };
            card.Controls.Add(lblNote);

            CardPanel.AddFieldLabel(card, "Brand", 18, 86);
            _cboBrand.Location = new Point(18, 106);
            _cboBrand.Width = 200;
            _cboBrand.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboBrand.Font = Theme.BaseFont;
            _cboBrand.Items.AddRange(new object[] { "Visa", "Mastercard", "American Express", "Discover" });
            _cboBrand.SelectedIndex = 0;
            card.Controls.Add(_cboBrand);

            CardPanel.AddFieldLabel(card, "Quantity", 230, 86);
            _numQuantity.Location = new Point(230, 106);
            _numQuantity.Width = 80;
            _numQuantity.Minimum = 1;
            _numQuantity.Maximum = 100;
            _numQuantity.Value = 5;
            _numQuantity.Font = Theme.BaseFont;
            card.Controls.Add(_numQuantity);

            _btnGenerate.Text = "Generate";
            _btnGenerate.Location = new Point(330, 104);
            _btnGenerate.Size = new Size(120, 30);
            Theme.StylePrimaryButton(_btnGenerate);
            _btnGenerate.Click += (_, _) => Generate();
            card.Controls.Add(_btnGenerate);

            _txtGenerated.Multiline = true;
            _txtGenerated.ReadOnly = true;
            _txtGenerated.ScrollBars = ScrollBars.Vertical;
            CardPanel.WrapWithBorder(card, _txtGenerated, new Point(18, 148), card.Width - 36,
                Math.Max(60, card.Height - 164),
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void ValidateCardNumber()
        {
            var number = _txtCardNumber.Text;
            if (string.IsNullOrWhiteSpace(number))
            {
                _lblValidateResult.ForeColor = Theme.Error;
                _lblValidateResult.Text = "Enter a card number first.";
                return;
            }

            var valid = CreditCardService.IsValidLuhn(number);
            var brand = CreditCardService.DetectBrand(number);
            _lblValidateResult.ForeColor = valid ? Theme.Success : Theme.Error;
            _lblValidateResult.Text = valid
                ? $"Valid - passes the Luhn checksum. Detected brand: {brand}"
                : $"Invalid - fails the Luhn checksum. Detected brand: {brand}";
        }

        private void Generate()
        {
            var brand = _cboBrand.SelectedItem?.ToString() ?? "Visa";
            var quantity = (int)_numQuantity.Value;
            var rng = new Random();

            var sb = new StringBuilder();
            sb.AppendLine($"{quantity} {brand} TEST number(s) - fake, for development/testing only:");
            sb.AppendLine();
            for (var i = 0; i < quantity; i++)
            {
                sb.AppendLine(CreditCardService.GenerateTestNumber(brand, rng));
            }
            _txtGenerated.Text = sb.ToString();
        }
    }
}
