using DevToolbox.UI;

namespace DevToolbox.Tools.HmacGenerator
{
    public class HmacGeneratorControl : UserControl
    {
        private readonly TextBox _txtMessage = new();
        private readonly TextBox _txtSecretKey = new();
        private readonly ComboBox _cboAlgorithm = new();
        private readonly Button _btnGenerate = new();
        private readonly Button _btnGenerateKey = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();
        private readonly ToolTip _toolTip = new();

        public HmacGeneratorControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it. Same-edge Dock=Top
            // siblings stack in reverse add-order, so the Message card (added last) ends up
            // visually on top, above the Secret Key / Algorithm card, above the output area.
            BuildOutputCard();
            BuildSecretCard();
            BuildMessageCard();
        }

        private void BuildMessageCard()
        {
            var card = CardPanel.Add(this, "MESSAGE", 220);
            _txtMessage.Multiline = true;
            _txtMessage.ScrollBars = ScrollBars.Vertical;
            _txtMessage.AcceptsReturn = true;
            _txtMessage.AcceptsTab = true;
            CardPanel.WrapWithBorder(card, _txtMessage, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void BuildSecretCard()
        {
            var card = CardPanel.Add(this, "SECRET KEY & ALGORITHM", 190);
            const int labelY = 44, fieldY = 64;

            var lblKey = CardPanel.AddFieldLabel(card, "Secret Key", 18, labelY);
            _txtSecretKey.Font = Theme.MonoFont;
            var keyWrapper = CardPanel.WrapWithBorder(card, _txtSecretKey, new Point(18, fieldY), 300, 30,
                AnchorStyles.Top | AnchorStyles.Left);

            var lblAlgorithm = CardPanel.AddFieldLabel(card, "Algorithm", 0, labelY);
            lblAlgorithm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cboAlgorithm.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboAlgorithm.Font = Theme.BaseFont;
            _cboAlgorithm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            foreach (var algorithm in HmacGeneratorService.Algorithms) _cboAlgorithm.Items.Add(algorithm);
            _cboAlgorithm.SelectedIndex = 2; // HMACSHA256
            card.Controls.Add(_cboAlgorithm);

            void PositionPairedFields()
            {
                var comboWidth = 220;
                var keyWidth = card.Width - 36 - comboWidth - 24;
                keyWrapper.Width = Math.Max(150, keyWidth);

                _cboAlgorithm.Width = comboWidth;
                _cboAlgorithm.Location = new Point(card.Width - 18 - comboWidth, fieldY);
                lblAlgorithm.Location = new Point(card.Width - 18 - lblAlgorithm.Width, labelY);
            }
            card.Resize += (_, _) => PositionPairedFields();
            PositionPairedFields();

            _btnGenerate.Text = "Generate HMAC";
            _btnGenerate.UseMnemonic = false;
            _btnGenerate.Location = new Point(18, 108);
            _btnGenerate.Size = new Size(160, 32);
            Theme.StylePrimaryButton(_btnGenerate);
            _btnGenerate.Click += (_, _) => TryGenerate();
            card.Controls.Add(_btnGenerate);

            _btnGenerateKey.Text = "Generate Random Key";
            _btnGenerateKey.UseMnemonic = false;
            _btnGenerateKey.Location = new Point(18 + _btnGenerate.Width + 10, 108);
            _btnGenerateKey.Size = new Size(190, 32);
            Theme.StyleSecondaryButton(_btnGenerateKey);
            _btnGenerateKey.Click += (_, _) => GenerateRandomKey();
            card.Controls.Add(_btnGenerateKey);
            _toolTip.SetToolTip(_btnGenerateKey, "Fills Secret Key with a cryptographically random key, sized for the selected algorithm - the same output format as `openssl rand -hex N`.");

            _lblError.Location = new Point(18, 150);
            _lblError.Size = new Size(card.Width - 36, 30);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "HMAC (lowercase hex)", 0, fill: true);

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

        /// <summary>Fills Secret Key with a fresh random key, sized to whichever algorithm is currently selected (32 random bytes -> 64 hex characters for HMACSHA256, matching `openssl rand -hex 32`).</summary>
        private void GenerateRandomKey()
        {
            var algorithm = _cboAlgorithm.SelectedItem as string ?? HmacGeneratorService.Algorithms[2];
            var byteLength = HmacGeneratorService.KeyByteLengthFor(algorithm);
            _txtSecretKey.Text = HmacGeneratorService.GenerateRandomKeyHex(byteLength);
        }

        private void TryGenerate()
        {
            try
            {
                var algorithm = _cboAlgorithm.SelectedItem as string ?? HmacGeneratorService.Algorithms[2];
                _txtOutput.Text = HmacGeneratorService.Compute(_txtMessage.Text, _txtSecretKey.Text, algorithm);
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
