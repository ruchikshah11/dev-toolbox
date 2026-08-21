using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.CompressPdf
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick a PDF, choose a compression
    /// preset, then save the recompressed copy via a Save As dialog. See
    /// <see cref="CompressPdfService"/> for exactly what this can and can't shrink.
    /// </summary>
    public class CompressPdfControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly RadioButton _radLow = new();
        private readonly RadioButton _radMedium = new();
        private readonly RadioButton _radHigh = new();
        private readonly Button _btnCompress = new();
        private readonly Label _lblNote = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;

        /// <summary>Builds the file-picker card and the compression-level/save card beneath it.</summary>
        public CompressPdfControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Same-edge Dock=Top siblings stack in reverse add-order (see PdfPasswordRemoverControl) -
            // the action card is added first so the source-file card (added last) ends up
            // visually on top of it.
            BuildActionCard();
            BuildSourceCard();
        }

        /// <summary>Builds the "choose a PDF" card with its file-name/size readout.</summary>
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

        /// <summary>Builds the compression-level radio buttons, the Compress button, the limitations note, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "COMPRESSION LEVEL & SAVE", 290);

            _radLow.Text = "Low compression (high quality)";
            _radLow.AutoSize = true;
            _radLow.Font = Theme.BaseFont;
            _radLow.ForeColor = Theme.Text;
            _radLow.BackColor = Theme.Card;
            _radLow.Location = new Point(18, 44);
            card.Controls.Add(_radLow);

            _radMedium.Text = "Medium compression";
            _radMedium.Checked = true;
            _radMedium.AutoSize = true;
            _radMedium.Font = Theme.BaseFont;
            _radMedium.ForeColor = Theme.Text;
            _radMedium.BackColor = Theme.Card;
            _radMedium.Location = new Point(18, 74);
            card.Controls.Add(_radMedium);

            _radHigh.Text = "High compression (low quality)";
            _radHigh.AutoSize = true;
            _radHigh.Font = Theme.BaseFont;
            _radHigh.ForeColor = Theme.Text;
            _radHigh.BackColor = Theme.Card;
            _radHigh.Location = new Point(18, 104);
            card.Controls.Add(_radHigh);

            _btnCompress.Text = "Compress & Save As...";
            _btnCompress.UseMnemonic = false;
            _btnCompress.Location = new Point(18, 142);
            _btnCompress.Size = new Size(180, 32);
            Theme.StylePrimaryButton(_btnCompress);
            _btnCompress.Click += OnCompressClick;
            card.Controls.Add(_btnCompress);

            _lblNote.Text = "Only shrinks embedded JPEG photos/images - a pure vector/text PDF, or one whose images "
                + "are already highly compressed, will shrink little or not at all.";
            _lblNote.ForeColor = Theme.TextMuted;
            _lblNote.BackColor = Theme.Card;
            _lblNote.Font = Theme.BaseFont;
            _lblNote.Location = new Point(18, 184);
            _lblNote.Size = new Size(card.Width - 36, 34);
            _lblNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblNote);

            _lblStatus.Location = new Point(18, 226);
            _lblStatus.Size = new Size(card.Width - 36, 50);
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
            var size = new FileInfo(_sourceFilePath).Length;
            _lblFileName.Text = $"{Path.GetFileName(_sourceFilePath)} ({FormatSize(size)})";
            HideStatus();
        }

        /// <summary>Validates the current selection, recompresses at the chosen level, and prompts for a Save As destination.</summary>
        private void OnCompressClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            var level = _radLow.Checked ? PdfCompressionLevel.LowCompressionHighQuality
                : _radHigh.Checked ? PdfCompressionLevel.HighCompressionLowQuality
                : PdfCompressionLevel.MediumCompression;

            using var dialog = new SaveFileDialog
            {
                Title = "Save compressed PDF as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + "-compressed.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var result = CompressPdfService.Compress(_sourceFilePath, dialog.FileName, level);
                var percent = result.OriginalSizeBytes > 0
                    ? 100.0 * (result.OriginalSizeBytes - result.CompressedSizeBytes) / result.OriginalSizeBytes
                    : 0;
                var imageNote = result.ImagesRecompressed == 0 && result.ImagesSkipped == 0
                    ? " (no embedded images found to recompress)"
                    : $" ({result.ImagesRecompressed} image(s) recompressed, {result.ImagesSkipped} left as-is)";
                ShowStatus(
                    $"Saved {FormatSize(result.OriginalSizeBytes)} -> {FormatSize(result.CompressedSizeBytes)} "
                    + $"({percent:0.#}% smaller){imageNote} to {dialog.FileName}",
                    isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Compression failed: {ex.Message}", isError: true);
            }
        }

        /// <summary>Formats a byte count as a human-readable size (B/KB/MB).</summary>
        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.#} MB"
        };

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
