using DevToolbox.UI;

namespace DevToolbox.Tools.AesEncryption
{
    /// <summary>
    /// Password-based AES-256-GCM encrypt/decrypt: paste text, enter a password, click Encrypt or
    /// Decrypt - both read from the same Input box and write to Output, matching this app's other
    /// two-direction encoder tools (see Base64EncoderTool). A dedicated control rather than the
    /// shared TextTransformControl since this needs an extra Password field alongside the text,
    /// which that shared control has no room for.
    /// </summary>
    public class AesEncryptionControl : UserControl
    {
        private readonly TextBox _txtInput = new();
        private readonly TextBox _txtPassword = new();
        private readonly Button _btnEncrypt = new();
        private readonly Button _btnDecrypt = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();

        public AesEncryptionControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it. Same-edge Dock=Top
            // siblings stack in reverse add-order, so the Input card (added last) ends up
            // visually on top, above the Password/Actions card, above the output area.
            BuildOutputCard();
            BuildPasswordCard();
            BuildInputCard();
        }

        /// <summary>Builds the plaintext/ciphertext input editor card.</summary>
        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "INPUT (plain text to encrypt, or Base64 to decrypt)", 220);
            _txtInput.Multiline = true;
            _txtInput.ScrollBars = ScrollBars.Vertical;
            _txtInput.AcceptsReturn = true;
            _txtInput.AcceptsTab = true;
            CardPanel.WrapWithBorder(card, _txtInput, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        /// <summary>Builds the password field, Encrypt/Decrypt buttons, and the error label.</summary>
        private void BuildPasswordCard()
        {
            var card = CardPanel.Add(this, "PASSWORD", 190);
            const int labelY = 44, fieldY = 64;

            CardPanel.AddFieldLabel(card, "Password", 18, labelY);
            _txtPassword.Font = Theme.MonoFont;
            // Dark-mode-safe colors set explicitly rather than left at WinForms defaults - this
            // exact class of bug (a password TextBox with ForeColor set but no matching BackColor,
            // or vice versa, so the masked dots are invisible against the default background) has
            // bitten this app before (Word to PDF's optional-password field).
            _txtPassword.ForeColor = Theme.Text;
            _txtPassword.BackColor = Theme.Card;
            _txtPassword.UseSystemPasswordChar = true;
            CardPanel.WrapWithBorder(card, _txtPassword, new Point(18, fieldY), 300, 30,
                AnchorStyles.Top | AnchorStyles.Left);

            _btnEncrypt.Text = "Encrypt";
            _btnEncrypt.UseMnemonic = false;
            _btnEncrypt.Location = new Point(18, 108);
            _btnEncrypt.Size = new Size(140, 32);
            Theme.StylePrimaryButton(_btnEncrypt);
            _btnEncrypt.Click += (_, _) => TryRun(AesEncryptionService.Encrypt);
            card.Controls.Add(_btnEncrypt);

            _btnDecrypt.Text = "Decrypt";
            _btnDecrypt.UseMnemonic = false;
            _btnDecrypt.Location = new Point(18 + _btnEncrypt.Width + 10, 108);
            _btnDecrypt.Size = new Size(140, 32);
            Theme.StyleSecondaryButton(_btnDecrypt);
            _btnDecrypt.Click += (_, _) => TryRun(AesEncryptionService.Decrypt);
            card.Controls.Add(_btnDecrypt);

            _lblError.Location = new Point(18, 150);
            _lblError.Size = new Size(card.Width - 36, 30);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        /// <summary>Builds the read-only result card with its Copy button.</summary>
        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "OUTPUT", 0, fill: true);

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

        /// <summary>Runs the given operation (Encrypt or Decrypt) against the current Input/Password and shows the result or the error.</summary>
        private void TryRun(Func<string, string, string> operation)
        {
            try
            {
                _txtOutput.Text = operation(_txtInput.Text, _txtPassword.Text);
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
