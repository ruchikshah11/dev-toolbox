using System.Text;
using DevToolbox.UI;

namespace DevToolbox.Tools.CertificateDecoder
{
    public class CertificateDecoderControl : UserControl
    {
        private readonly TextBox _txtInput = new();
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();

        /// <summary>Builds the paste/upload input card and the decoded-details output card.</summary>
        public CertificateDecoderControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking order
            // note used throughout the tool controls.
            BuildOutputCard();
            BuildInputCard();

            Decode();
        }

        /// <summary>Builds the certificate paste box plus its "choose a file" row and error label.</summary>
        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "CERTIFICATE (paste PEM / base64 DER, or choose a file below)", 240);

            _txtInput.Multiline = true;
            _txtInput.ScrollBars = ScrollBars.Vertical;
            _txtInput.AcceptsReturn = true;
            _txtInput.AcceptsTab = true;
            _txtInput.TextChanged += (_, _) => Decode();
            // Reserves room below the paste box for BOTH the error label and the button row (a
            // prior version only reserved space for the button row, so the error label - carved
            // out of that same 40px strip via a negative offset - visually overlapped the paste
            // box's own bottom border instead of sitting in a clear gap beneath it).
            CardPanel.WrapWithBorder(card, _txtInput, new Point(18, 42), card.Width - 36, card.Height - 118,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);

            _btnChooseFile.Text = "Choose File";
            _btnChooseFile.Size = new Size(120, 30);
            _btnChooseFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += OnChooseFileClick;
            card.Controls.Add(_btnChooseFile);

            _lblFileName.Text = "No file chosen";
            _lblFileName.ForeColor = Theme.TextMuted;
            _lblFileName.Font = Theme.BaseFont;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblFileName);

            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);

            void PositionBottomRow()
            {
                var rowTop = card.Height - 38;
                _btnChooseFile.Location = new Point(18, rowTop);
                _lblFileName.Location = new Point(150, rowTop + 5);
                _lblFileName.Size = new Size(Math.Max(40, card.Width - 36 - 150), 22);
                _lblError.Location = new Point(18, rowTop - 30);
                _lblError.Size = new Size(card.Width - 36, 24);
            }
            card.Resize += (_, _) => PositionBottomRow();
            PositionBottomRow();
        }

        /// <summary>Builds the read-only decoded-details output card with its Copy button.</summary>
        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "DECODED CERTIFICATE", 0, fill: true);

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

        /// <summary>Opens a file picker, loads the chosen certificate, and reflects its text into the paste box.</summary>
        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose a certificate file",
                Filter = "Certificate files (*.cer;*.crt;*.pem;*.der)|*.cer;*.crt;*.pem;*.der|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            var bytes = File.ReadAllBytes(dialog.FileName);
            _lblFileName.Text = Path.GetFileName(dialog.FileName);

            // Feeds the file back through the same paste-based path (ExtractBytes) so there's
            // one decode code path to maintain: PEM text files show as-is, raw binary DER falls
            // back to its base64 form.
            _txtInput.Text = TryReadAsPemText(bytes) ?? Convert.ToBase64String(bytes);
        }

        /// <summary>Returns the file's text if it's a PEM-formatted certificate, otherwise null for binary DER.</summary>
        private static string? TryReadAsPemText(byte[] bytes)
        {
            try
            {
                var text = Encoding.UTF8.GetString(bytes);
                return text.Contains("-----BEGIN") ? text : null;
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }

        /// <summary>Decodes the current paste-box contents and shows the summary or the error.</summary>
        private void Decode()
        {
            try
            {
                var info = CertificateDecoderService.Decode(_txtInput.Text);
                _txtOutput.Text = CertificateDecoderService.FormatSummary(info);
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
