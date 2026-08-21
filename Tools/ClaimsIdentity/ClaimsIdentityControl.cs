using DevToolbox.UI;

namespace DevToolbox.Tools.ClaimsIdentity
{
    public class ClaimsIdentityControl : UserControl
    {
        private readonly TextBox _txtDecodeInput = new();
        private readonly TextBox _txtDecodeResult = new();

        private readonly ComboBox _cboClaimType = new();
        private readonly TextBox _txtEncodeValue = new();
        private readonly Button _btnEncode = new();
        private readonly TextBox _txtEncodeResult = new();
        private readonly Label _lblEncodeError = new();

        public ClaimsIdentityControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking order
            // note in TextTransformControl/MainForm. Decode (Top) ends up above Encode (Fill).
            var encodeCard = CardPanel.Add(this, "Build a Claims-Encoded Identity", 0, fill: true);
            BuildEncodeCard(encodeCard);

            BuildDecodeCard();
        }

        private void BuildDecodeCard()
        {
            var card = CardPanel.Add(this, "Decode a Claims-Encoded Identity", 260);

            CardPanel.AddFieldLabel(card, "Claims string (e.g. i:0#.f|membership|user@domain.com)", 18, 44);
            _txtDecodeInput.Font = Theme.MonoFont;
            _txtDecodeInput.Location = new Point(18, 64);
            _txtDecodeInput.Width = card.Width - 36;
            _txtDecodeInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtDecodeInput.TextChanged += (_, _) => Decode();
            card.Controls.Add(_txtDecodeInput);

            _txtDecodeResult.Multiline = true;
            _txtDecodeResult.ReadOnly = true;
            _txtDecodeResult.ScrollBars = ScrollBars.Vertical;
            CardPanel.WrapWithBorder(card, _txtDecodeResult, new Point(18, 96), card.Width - 36, card.Height - 112,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void BuildEncodeCard(Panel card)
        {
            CardPanel.AddFieldLabel(card, "Claim Type", 18, 44);
            _cboClaimType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboClaimType.Font = Theme.BaseFont;
            _cboClaimType.Location = new Point(18, 64);
            _cboClaimType.Width = 320;
            foreach (var claimType in ClaimsIdentityService.ClaimTypes) _cboClaimType.Items.Add(claimType);
            _cboClaimType.SelectedIndex = 0;
            _cboClaimType.SelectedIndexChanged += (_, _) => UpdateValueFieldState();
            card.Controls.Add(_cboClaimType);

            CardPanel.AddFieldLabel(card, "Value", 356, 44);
            _txtEncodeValue.Font = Theme.MonoFont;
            _txtEncodeValue.Location = new Point(356, 64);
            _txtEncodeValue.Width = 260;
            _txtEncodeValue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_txtEncodeValue);

            _btnEncode.Text = "Build Claim String";
            _btnEncode.UseMnemonic = false;
            _btnEncode.Location = new Point(18, 104);
            _btnEncode.Size = new Size(160, 32);
            Theme.StylePrimaryButton(_btnEncode);
            _btnEncode.Click += (_, _) => TryEncode();
            card.Controls.Add(_btnEncode);

            _lblEncodeError.Location = new Point(190, 108);
            _lblEncodeError.Size = new Size(card.Width - 208, 26);
            _lblEncodeError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblEncodeError.ForeColor = Theme.Error;
            _lblEncodeError.Font = Theme.BaseFont;
            _lblEncodeError.AutoEllipsis = true;
            _lblEncodeError.Visible = false;
            card.Controls.Add(_lblEncodeError);

            CardPanel.AddFieldLabel(card, "Result", 18, 154);

            var btnCopy = new Button { Text = "Copy", Size = new Size(90, 26), Location = new Point(card.Width - 18 - 90, 150) };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopy.Click += (_, _) =>
            {
                if (_txtEncodeResult.Text.Length > 0) Clipboard.SetText(_txtEncodeResult.Text);
            };
            card.Controls.Add(btnCopy);

            _txtEncodeResult.ReadOnly = true;
            _txtEncodeResult.Font = Theme.MonoFont;
            CardPanel.WrapWithBorder(card, _txtEncodeResult, new Point(18, 174), card.Width - 36, 34,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);

            UpdateValueFieldState();
        }

        private void UpdateValueFieldState()
        {
            if (_cboClaimType.SelectedItem is not ClaimTypeInfo claimType) return;
            _txtEncodeValue.Enabled = claimType.HasValue;
            if (!claimType.HasValue) _txtEncodeValue.Text = string.Empty;
        }

        private void Decode()
        {
            if (string.IsNullOrWhiteSpace(_txtDecodeInput.Text))
            {
                _txtDecodeResult.Text = string.Empty;
                return;
            }

            var result = ClaimsIdentityService.Decode(_txtDecodeInput.Text);
            _txtDecodeResult.Text = string.Join("\r\n", new[]
            {
                $"Claim Type: {result.ClaimType}",
                $"Prefix: {result.Prefix}",
                $"Value: {result.Value}"
            });
        }

        private void TryEncode()
        {
            try
            {
                var claimType = (ClaimTypeInfo)_cboClaimType.SelectedItem!;
                _txtEncodeResult.Text = ClaimsIdentityService.Encode(claimType, _txtEncodeValue.Text);
                HideEncodeError();
            }
            catch (FormatException ex)
            {
                _txtEncodeResult.Text = string.Empty;
                ShowEncodeError(ex.Message);
            }
        }

        private void ShowEncodeError(string message)
        {
            _lblEncodeError.Text = message;
            _lblEncodeError.Visible = true;
        }

        private void HideEncodeError() => _lblEncodeError.Visible = false;
    }
}
