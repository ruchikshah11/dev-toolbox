using DevToolbox.Tools.HmacGenerator;
using DevToolbox.UI;

namespace DevToolbox.Tools.JwtEncoder
{
    public class JwtEncoderControl : UserControl
    {
        private readonly TextBox _txtPayload = new();
        private readonly TextBox _txtSecretKey = new();
        private readonly ComboBox _cboAlgorithm = new();
        private readonly Button _btnGenerate = new();
        private readonly Button _btnGenerateKey = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();
        private readonly ToolTip _toolTip = new();

        // JWT's HS256/384/512 names don't match HmacGeneratorService.KeyByteLengthFor's
        // "HMACSHA256"-style names, but the underlying ideal key size (matching the hash's own
        // output size) is identical - reusing that same reasoning/table here rather than
        // duplicating GenerateRandomKeyHex's CSPRNG logic in a second place.
        private static int KeyByteLengthFor(string algorithm) => algorithm switch
        {
            "HS256" => 32,
            "HS384" => 48,
            "HS512" => 64,
            _ => 32
        };

        /// <summary>Builds the payload/secret cards and the signed-token output card.</summary>
        public JwtEncoderControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it. Same-edge Dock=Top
            // siblings stack in reverse add-order, so the Payload card (added last) ends up
            // visually on top, above the Secret Key / Algorithm card, above the output area.
            BuildOutputCard();
            BuildSecretCard();
            BuildPayloadCard();
        }

        /// <summary>Builds the JSON claims payload editor card, prefilled with a runnable example.</summary>
        private void BuildPayloadCard()
        {
            var card = CardPanel.Add(this, "PAYLOAD (JSON claims)", 220);
            _txtPayload.Multiline = true;
            _txtPayload.ScrollBars = ScrollBars.Vertical;
            _txtPayload.AcceptsReturn = true;
            _txtPayload.AcceptsTab = true;
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _txtPayload.Text = "{\r\n  \"sub\": \"user123\",\r\n  \"name\": \"Jane Doe\",\r\n  \"iat\": " + nowUnix + "\r\n}";
            CardPanel.WrapWithBorder(card, _txtPayload, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        /// <summary>Builds the secret-key + algorithm row, the Generate button, and the error label.</summary>
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
            foreach (var algorithm in JwtEncoderService.Algorithms) _cboAlgorithm.Items.Add(algorithm);
            _cboAlgorithm.SelectedIndex = 0; // HS256
            card.Controls.Add(_cboAlgorithm);

            void PositionPairedFields()
            {
                var comboWidth = 160;
                var keyWidth = card.Width - 36 - comboWidth - 24;
                keyWrapper.Width = Math.Max(150, keyWidth);

                _cboAlgorithm.Width = comboWidth;
                _cboAlgorithm.Location = new Point(card.Width - 18 - comboWidth, fieldY);
                lblAlgorithm.Location = new Point(card.Width - 18 - lblAlgorithm.Width, labelY);
            }
            card.Resize += (_, _) => PositionPairedFields();
            PositionPairedFields();

            _btnGenerate.Text = "Generate JWT";
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

        /// <summary>Builds the read-only signed-token output card with its Copy button.</summary>
        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "SIGNED JWT", 0, fill: true);

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

        /// <summary>Fills Secret Key with a fresh random key, sized to whichever algorithm is currently selected (32 random bytes -> 64 hex characters for HS256, matching `openssl rand -hex 32`).</summary>
        private void GenerateRandomKey()
        {
            var algorithm = _cboAlgorithm.SelectedItem as string ?? JwtEncoderService.Algorithms[0];
            var byteLength = KeyByteLengthFor(algorithm);
            _txtSecretKey.Text = HmacGeneratorService.GenerateRandomKeyHex(byteLength);
        }

        /// <summary>Runs the encoder against the current inputs and shows the token or the error.</summary>
        private void TryGenerate()
        {
            try
            {
                var algorithm = _cboAlgorithm.SelectedItem as string ?? JwtEncoderService.Algorithms[0];
                _txtOutput.Text = JwtEncoderService.Encode(_txtPayload.Text, _txtSecretKey.Text, algorithm);
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
