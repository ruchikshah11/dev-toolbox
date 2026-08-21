using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.PdfToWord
{
    /// <summary>
    /// File-based tool (no paste box - .docx/.pdf are binary formats): pick a PDF, convert it,
    /// then save the extracted-text .docx anywhere via a Save As dialog.
    /// </summary>
    public class PdfToWordControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly Button _btnConvert = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;

        /// <summary>Builds the file-picker card and the convert/save card beneath it.</summary>
        public PdfToWordControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Same-edge Dock=Top siblings stack in reverse add-order (see
            // PdfPasswordRemoverControl/WordToPdfControl) - the action card is added first so
            // the source-file card (added last) ends up visually on top of it.
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
            _lblFileName.Font = Theme.BaseFont;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Location = new Point(170, 44);
            _lblFileName.Size = new Size(card.Width - 36 - 170, 32);
            _lblFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblFileName);
        }

        /// <summary>Builds the Convert button, its limitations note, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "CONVERT & SAVE", 190);

            _btnConvert.Text = "Convert to Word & Save As...";
            _btnConvert.UseMnemonic = false;
            _btnConvert.Location = new Point(18, 44);
            _btnConvert.Size = new Size(220, 32);
            Theme.StylePrimaryButton(_btnConvert);
            _btnConvert.Click += OnConvertClick;
            card.Controls.Add(_btnConvert);

            var lblNote = new Label
            {
                Text = "Extracts page text as plain paragraphs. Original fonts, columns, tables, images, "
                       + "and scanned (image-only) pages are not preserved.",
                ForeColor = Theme.TextMuted,
                Font = Theme.BaseFont,
                Location = new Point(18, 88),
                Size = new Size(card.Width - 36, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(lblNote);

            _lblStatus.Location = new Point(18, 146);
            _lblStatus.Size = new Size(card.Width - 36, 34);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = Theme.BaseFont;
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

        /// <summary>Validates the current selection, converts, and prompts for a Save As destination.</summary>
        private void OnConvertClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save converted Word document as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + ".docx",
                Filter = "Word documents (*.docx)|*.docx"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                PdfToWordService.Convert(_sourceFilePath, dialog.FileName);
                ShowStatus($"Saved Word document to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Conversion failed: {ex.Message}", isError: true);
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
