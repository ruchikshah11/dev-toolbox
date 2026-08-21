using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.ProtectPdf
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick an unencrypted PDF, enter a
    /// user and/or owner password, then save an encrypted copy via a Save As dialog.
    /// </summary>
    public class ProtectPdfControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly TextBox _txtUserPassword = new();
        private readonly TextBox _txtOwnerPassword = new();
        private readonly Button _btnProtect = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;

        /// <summary>Builds the file-picker card and the password/save card beneath it.</summary>
        public ProtectPdfControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Same-edge Dock=Top siblings stack in reverse add-order (see PdfPasswordRemoverControl) -
            // the action card is added first so the source-file card (added last) ends up
            // visually on top of it.
            BuildActionCard();
            BuildSourceCard();
        }

        /// <summary>Builds the "choose a PDF" card with its file-name readout.</summary>
        private void BuildSourceCard()
        {
            var card = CardPanel.Add(this, "PDF FILE", 100);

            _btnChooseFile.Text = "Choose File...";
            _btnChooseFile.UseMnemonic = false;
            _btnChooseFile.Location = new Point(18, 44);
            _btnChooseFile.Size = new Size(140, 32);
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += OnChooseFileClick;
            card.Controls.Add(_btnChooseFile);

            _lblFileName.Text = "No file chosen";
            _lblFileName.ForeColor = Theme.TextMuted;
            _lblFileName.BackColor = Theme.Card;
            _lblFileName.Font = Theme.BaseFont;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Location = new Point(170, 44);
            _lblFileName.Size = new Size(card.Width - 36 - 170, 32);
            _lblFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblFileName);
        }

        /// <summary>Builds the user/owner password fields, the Protect button, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "PASSWORD & SAVE", 270);

            CardPanel.AddFieldLabel(card, "User password (required to open the PDF - optional)", 18, 44);
            _txtUserPassword.Location = new Point(18, 64);
            _txtUserPassword.Width = 320;
            _txtUserPassword.Font = Theme.BaseFont;
            _txtUserPassword.ForeColor = Theme.Text;
            _txtUserPassword.BackColor = Theme.Card;
            _txtUserPassword.UseSystemPasswordChar = true;
            card.Controls.Add(_txtUserPassword);

            CardPanel.AddFieldLabel(card, "Owner password (required to change permissions - optional)", 18, 106);
            _txtOwnerPassword.Location = new Point(18, 126);
            _txtOwnerPassword.Width = 320;
            _txtOwnerPassword.Font = Theme.BaseFont;
            _txtOwnerPassword.ForeColor = Theme.Text;
            _txtOwnerPassword.BackColor = Theme.Card;
            _txtOwnerPassword.UseSystemPasswordChar = true;
            card.Controls.Add(_txtOwnerPassword);

            _btnProtect.Text = "Protect & Save As...";
            _btnProtect.UseMnemonic = false;
            _btnProtect.Location = new Point(18, 164);
            _btnProtect.Size = new Size(180, 32);
            Theme.StylePrimaryButton(_btnProtect);
            _btnProtect.Click += OnProtectClick;
            card.Controls.Add(_btnProtect);

            _lblStatus.Location = new Point(18, 206);
            _lblStatus.Size = new Size(card.Width - 36, 34);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = Theme.BaseFont;
            _lblStatus.BackColor = Theme.Card;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.Visible = false;
            card.Controls.Add(_lblStatus);
        }

        /// <summary>Opens a file picker for the source PDF.</summary>
        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose a PDF file to protect",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _sourceFilePath = dialog.FileName;
            _lblFileName.Text = Path.GetFileName(_sourceFilePath);
            HideStatus();
        }

        /// <summary>Validates the current selection/passwords, encrypts, and prompts for a Save As destination.</summary>
        private void OnProtectClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            if (string.IsNullOrEmpty(_txtUserPassword.Text) && string.IsNullOrEmpty(_txtOwnerPassword.Text))
            {
                ShowStatus("Enter a user password, an owner password, or both.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save protected PDF as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + "-protected.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                ProtectPdfService.Protect(_sourceFilePath, dialog.FileName, _txtUserPassword.Text, _txtOwnerPassword.Text);
                ShowStatus($"Saved protected PDF to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Protecting the PDF failed: {ex.Message}", isError: true);
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
