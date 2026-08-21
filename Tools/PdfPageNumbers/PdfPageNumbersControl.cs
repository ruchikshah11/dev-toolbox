using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.PdfPageNumbers
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick a PDF, choose where the
    /// "Page X of N" label goes, then save the stamped copy via a Save As dialog.
    /// </summary>
    public class PdfPageNumbersControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly ComboBox _cboPosition = new();
        private readonly Button _btnAdd = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;

        // Display order matches PageNumberPosition's declaration order, so SelectedIndex casts directly.
        private static readonly string[] PositionLabels =
        {
            "Bottom Left", "Bottom Center", "Bottom Right", "Top Left", "Top Center", "Top Right"
        };

        /// <summary>Builds the file-picker card and the position/save card beneath it.</summary>
        public PdfPageNumbersControl()
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

        /// <summary>Builds the position dropdown, the Add Page Numbers button, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "POSITION & SAVE", 190);

            CardPanel.AddFieldLabel(card, "Position", 18, 44);
            _cboPosition.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboPosition.Font = Theme.BaseFont;
            _cboPosition.Location = new Point(18, 64);
            _cboPosition.Width = 160;
            _cboPosition.Items.AddRange(PositionLabels);
            _cboPosition.SelectedIndex = 1; // Bottom Center
            card.Controls.Add(_cboPosition);

            _btnAdd.Text = "Add Page Numbers & Save As...";
            _btnAdd.UseMnemonic = false;
            _btnAdd.Location = new Point(18, 104);
            _btnAdd.Size = new Size(240, 32);
            Theme.StylePrimaryButton(_btnAdd);
            _btnAdd.Click += OnAddClick;
            card.Controls.Add(_btnAdd);

            _lblStatus.Location = new Point(18, 146);
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
                Title = "Choose a PDF file",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _sourceFilePath = dialog.FileName;
            _lblFileName.Text = Path.GetFileName(_sourceFilePath);
            HideStatus();
        }

        /// <summary>Validates the current selection, stamps every page, and prompts for a Save As destination.</summary>
        private void OnAddClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save numbered PDF as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + "-numbered.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var position = (PageNumberPosition)_cboPosition.SelectedIndex;
                PdfPageNumbersService.AddPageNumbers(_sourceFilePath, dialog.FileName, position);
                ShowStatus($"Saved numbered PDF to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Adding page numbers failed: {ex.Message}", isError: true);
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
