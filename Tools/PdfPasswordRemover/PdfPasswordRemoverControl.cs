using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.PdfPasswordRemover
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick an encrypted PDF, type its
    /// password, then save an unencrypted copy alongside/anywhere via a Save As dialog.
    /// </summary>
    public class PdfPasswordRemoverControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly TextBox _txtPassword = new();
        private readonly Button _btnRemove = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;

        /// <summary>Builds the file-picker card and the password/remove-and-save card beneath it.</summary>
        public PdfPasswordRemoverControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Same-edge Dock=Top siblings stack in reverse add-order (see
            // FileEncodingConverterControl) - the action card is added first so the source-file
            // card (added last) ends up visually on top of it.
            BuildActionCard();
            BuildSourceCard();
        }

        /// <summary>Builds the "choose an encrypted PDF" card with its file-name readout.</summary>
        private void BuildSourceCard()
        {
            var card = CardPanel.Add(this, "ENCRYPTED PDF FILE", 100);

            _btnChooseFile.Text = "Choose File...";
            _btnChooseFile.UseMnemonic = false;
            _btnChooseFile.Location = new Point(18, 44);
            _btnChooseFile.Size = new Size(140, 32);
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += OnChooseFileClick;
            card.Controls.Add(_btnChooseFile);

            _lblFileName.Text = "No file chosen";
            _lblFileName.ForeColor = Theme.TextMuted;
            _lblFileName.Font = Theme.BaseFont;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Location = new Point(170, 44);
            _lblFileName.Size = new Size(card.Width - 36 - 170, 32);
            _lblFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblFileName);
        }

        /// <summary>Builds the password field, the Remove Password button, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "PASSWORD & SAVE", 190);

            CardPanel.AddFieldLabel(card, "PDF password (user or owner password)", 18, 44);
            _txtPassword.Location = new Point(18, 64);
            _txtPassword.Width = 320;
            _txtPassword.Font = Theme.BaseFont;
            _txtPassword.ForeColor = Theme.Text;
            _txtPassword.BackColor = Theme.Card;
            _txtPassword.UseSystemPasswordChar = true;
            card.Controls.Add(_txtPassword);

            _btnRemove.Text = "Remove Password & Save As...";
            _btnRemove.UseMnemonic = false;
            _btnRemove.Location = new Point(18, 104);
            _btnRemove.Size = new Size(220, 32);
            Theme.StylePrimaryButton(_btnRemove);
            _btnRemove.Click += OnRemoveClick;
            card.Controls.Add(_btnRemove);

            _lblStatus.Location = new Point(18, 146);
            _lblStatus.Size = new Size(card.Width - 36, 34);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = Theme.BaseFont;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.Visible = false;
            card.Controls.Add(_lblStatus);
        }

        /// <summary>Opens a file picker for the encrypted source PDF.</summary>
        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose an encrypted PDF file",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _sourceFilePath = dialog.FileName;
            _lblFileName.Text = Path.GetFileName(_sourceFilePath);
            HideStatus();
        }

        /// <summary>Validates the current selection/password, decrypts, and prompts for a Save As destination.</summary>
        private void OnRemoveClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            if (string.IsNullOrEmpty(_txtPassword.Text))
            {
                ShowStatus("Enter the PDF's password first.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save decrypted PDF as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + "-decrypted.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                PdfPasswordRemoverService.RemovePassword(_sourceFilePath, _txtPassword.Text, dialog.FileName);
                ShowStatus($"Saved unlocked copy to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Could not remove password: {ex.Message}", isError: true);
            }
        }

        /// <summary>Shows the status label in either the success (green) or error (red) color.</summary>
        private void ShowStatus(string message, bool isError)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = isError ? Theme.Error : Theme.Success;
            _lblStatus.Visible = true;
        }

        /// <summary>Hides the status label.</summary>
        private void HideStatus() => _lblStatus.Visible = false;
    }
}
