using DevToolbox.UI;

namespace DevToolbox.Tools.JwtDecoder
{
    public class JwtDecoderControl : UserControl
    {
        private readonly TextBox _txtToken = new();
        private readonly TextBox _txtSecret = new();
        private readonly Label _lblSignatureStatus = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtHeader = new();
        private readonly TextBox _txtPayload = new();

        public JwtDecoderControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it. Same-edge Dock=Top
            // siblings stack in reverse add-order, so the Token card (added last) ends up
            // visually on top, above Signature Verification, above Header, above Payload.
            BuildPayloadCard();
            BuildHeaderCard();
            BuildSecretCard();
            BuildTokenCard();

            Decode();
        }

        private void BuildTokenCard()
        {
            var card = CardPanel.Add(this, "JWT (paste the full token, e.g. eyJhbGc...)", 150);
            _txtToken.Multiline = true;
            _txtToken.ScrollBars = ScrollBars.Vertical;
            _txtToken.AcceptsReturn = true;
            _txtToken.AcceptsTab = true;
            _txtToken.TextChanged += (_, _) => Decode();
            CardPanel.WrapWithBorder(card, _txtToken, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void BuildSecretCard()
        {
            var card = CardPanel.Add(this, "SIGNATURE VERIFICATION (optional)", 160);

            CardPanel.AddFieldLabel(card, "Secret Key (for HS256 / HS384 / HS512)", 18, 44);
            _txtSecret.Font = Theme.MonoFont;
            _txtSecret.Location = new Point(18, 64);
            _txtSecret.Width = 300;
            _txtSecret.TextChanged += (_, _) => UpdateSignatureStatus();
            card.Controls.Add(_txtSecret);

            _lblSignatureStatus.Location = new Point(330, 64);
            _lblSignatureStatus.Size = new Size(card.Width - 348, 26);
            _lblSignatureStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblSignatureStatus.Font = Theme.BoldFont;
            _lblSignatureStatus.ForeColor = Theme.TextMuted;
            _lblSignatureStatus.Text = "Enter a secret to verify HS256/384/512 signatures.";
            _lblSignatureStatus.AutoEllipsis = true;
            _lblSignatureStatus.UseMnemonic = false;
            card.Controls.Add(_lblSignatureStatus);

            _lblError.Location = new Point(18, 100);
            _lblError.Size = new Size(card.Width - 36, 24);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        private void BuildHeaderCard()
        {
            var card = CardPanel.Add(this, "Header", 140);
            _txtHeader.Multiline = true;
            _txtHeader.ReadOnly = true;
            _txtHeader.ScrollBars = ScrollBars.Vertical;
            CardPanel.WrapWithBorder(card, _txtHeader, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void BuildPayloadCard()
        {
            var card = CardPanel.Add(this, "Payload", 0, fill: true);

            var btnCopy = new Button { Text = "Copy Payload", Size = new Size(130, 28) };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (_, _) =>
            {
                if (_txtPayload.Text.Length > 0) Clipboard.SetText(_txtPayload.Text);
            };
            card.Controls.Add(btnCopy);

            void PositionCopy() => btnCopy.Location = new Point(card.Width - 18 - btnCopy.Width, 8);
            card.Resize += (_, _) => PositionCopy();
            PositionCopy();

            _txtPayload.Multiline = true;
            _txtPayload.ReadOnly = true;
            _txtPayload.ScrollBars = ScrollBars.Vertical;
            CardPanel.WrapWithBorder(card, _txtPayload, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void Decode()
        {
            try
            {
                var result = JwtDecoderService.Decode(_txtToken.Text);
                _txtHeader.Text = result.HeaderJson;
                _txtPayload.Text = result.PayloadJson + "\r\n\r\n--- Claims ---\r\n" + result.ClaimsSummary;
                HideError();
            }
            catch (Exception ex)
            {
                _txtHeader.Text = string.Empty;
                _txtPayload.Text = string.Empty;
                ShowError(ex.Message);
            }

            UpdateSignatureStatus();
        }

        private void UpdateSignatureStatus()
        {
            if (string.IsNullOrWhiteSpace(_txtToken.Text) || _txtSecret.Text.Length == 0)
            {
                _lblSignatureStatus.ForeColor = Theme.TextMuted;
                _lblSignatureStatus.Text = "Enter a secret to verify HS256/384/512 signatures.";
                return;
            }

            try
            {
                var verified = JwtDecoderService.VerifySignature(_txtToken.Text, _txtSecret.Text);
                if (verified is null)
                {
                    _lblSignatureStatus.ForeColor = Theme.TextMuted;
                    _lblSignatureStatus.Text = "Algorithm is not HS256/384/512 - cannot verify with a shared secret.";
                }
                else
                {
                    _lblSignatureStatus.ForeColor = verified.Value ? Theme.Success : Theme.Error;
                    _lblSignatureStatus.Text = verified.Value ? "Signature valid." : "Signature does NOT match.";
                }
            }
            catch (FormatException)
            {
                _lblSignatureStatus.ForeColor = Theme.TextMuted;
                _lblSignatureStatus.Text = string.Empty;
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
