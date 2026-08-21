using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.SplitPdf
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick a PDF, choose either "extract
    /// a page range" (saved as one new PDF via Save As) or "split every page into its own file"
    /// (saved into a chosen folder via a folder picker).
    /// </summary>
    public class SplitPdfControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly RadioButton _radRange = new();
        private readonly RadioButton _radEveryPage = new();
        private readonly Label _lblFrom = new();
        private readonly NumericUpDown _numFrom = new();
        private readonly Label _lblTo = new();
        private readonly NumericUpDown _numTo = new();
        private readonly Button _btnSplit = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;
        private int _pageCount;

        /// <summary>Builds the file-picker card and the split-mode/action card beneath it.</summary>
        public SplitPdfControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Same-edge Dock=Top siblings stack in reverse add-order (see PdfPasswordRemoverControl) -
            // the action card is added first so the source-file card (added last) ends up
            // visually on top of it.
            BuildActionCard();
            BuildSourceCard();
        }

        /// <summary>Builds the "choose a PDF" card with its file-name/page-count readout.</summary>
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

        /// <summary>Builds the split-mode radio buttons, the range fields, the Split button, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "SPLIT MODE & SAVE", 260);

            _radRange.Text = "Extract a page range into one new PDF";
            _radRange.Checked = true;
            _radRange.AutoSize = true;
            _radRange.Font = Theme.BaseFont;
            _radRange.ForeColor = Theme.Text;
            _radRange.BackColor = Theme.Card;
            _radRange.Location = new Point(18, 44);
            _radRange.CheckedChanged += (_, _) => UpdateRangeFieldsEnabled();
            card.Controls.Add(_radRange);

            _lblFrom.Text = "From page";
            _lblFrom.ForeColor = Theme.TextMuted;
            _lblFrom.BackColor = Theme.Card;
            _lblFrom.Font = Theme.BaseFont;
            _lblFrom.AutoSize = true;
            _lblFrom.Location = new Point(40, 74);
            card.Controls.Add(_lblFrom);

            _numFrom.Location = new Point(40, 94);
            _numFrom.Width = 80;
            _numFrom.Minimum = 1;
            _numFrom.Maximum = 1;
            _numFrom.Value = 1;
            _numFrom.Font = Theme.BaseFont;
            card.Controls.Add(_numFrom);

            _lblTo.Text = "To page";
            _lblTo.ForeColor = Theme.TextMuted;
            _lblTo.BackColor = Theme.Card;
            _lblTo.Font = Theme.BaseFont;
            _lblTo.AutoSize = true;
            _lblTo.Location = new Point(150, 74);
            card.Controls.Add(_lblTo);

            _numTo.Location = new Point(150, 94);
            _numTo.Width = 80;
            _numTo.Minimum = 1;
            _numTo.Maximum = 1;
            _numTo.Value = 1;
            _numTo.Font = Theme.BaseFont;
            card.Controls.Add(_numTo);

            _radEveryPage.Text = "Split every page into its own file (choose a destination folder)";
            _radEveryPage.AutoSize = true;
            _radEveryPage.Font = Theme.BaseFont;
            _radEveryPage.ForeColor = Theme.Text;
            _radEveryPage.BackColor = Theme.Card;
            _radEveryPage.Location = new Point(18, 132);
            card.Controls.Add(_radEveryPage);

            _btnSplit.Text = "Split...";
            _btnSplit.UseMnemonic = false;
            _btnSplit.Location = new Point(18, 170);
            _btnSplit.Size = new Size(140, 32);
            Theme.StylePrimaryButton(_btnSplit);
            _btnSplit.Click += OnSplitClick;
            card.Controls.Add(_btnSplit);

            _lblStatus.Location = new Point(18, 212);
            _lblStatus.Size = new Size(card.Width - 36, 34);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = Theme.BaseFont;
            _lblStatus.BackColor = Theme.Card;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.Visible = false;
            card.Controls.Add(_lblStatus);
        }

        /// <summary>Enables the From/To range fields only when the "extract a range" mode is selected.</summary>
        private void UpdateRangeFieldsEnabled()
        {
            _numFrom.Enabled = _radRange.Checked;
            _numTo.Enabled = _radRange.Checked;
        }

        /// <summary>Opens a file picker for the source PDF and reads its page count for range validation.</summary>
        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose a PDF file",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                _pageCount = SplitPdfService.GetPageCount(dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowStatus($"Could not read that PDF: {ex.Message}", isError: true);
                return;
            }

            _sourceFilePath = dialog.FileName;
            _lblFileName.Text = $"{Path.GetFileName(_sourceFilePath)} ({_pageCount} page{(_pageCount == 1 ? "" : "s")})";

            _numFrom.Maximum = _pageCount;
            _numTo.Maximum = _pageCount;
            _numFrom.Value = 1;
            _numTo.Value = _pageCount;
            HideStatus();
        }

        /// <summary>Runs whichever split mode is selected, prompting for the appropriate Save As file or destination folder.</summary>
        private void OnSplitClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            if (_radRange.Checked)
            {
                SplitRange();
            }
            else
            {
                SplitEveryPage();
            }
        }

        /// <summary>Validates the From/To range and extracts it into one new PDF via a Save As dialog.</summary>
        private void SplitRange()
        {
            var first = (int)_numFrom.Value;
            var last = (int)_numTo.Value;
            if (last < first)
            {
                ShowStatus("'To page' must be greater than or equal to 'From page'.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save extracted pages as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + $"-pages-{first}-{last}.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                SplitPdfService.ExtractRange(_sourceFilePath!, first, last, dialog.FileName);
                ShowStatus($"Saved pages {first}-{last} to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Split failed: {ex.Message}", isError: true);
            }
        }

        /// <summary>Prompts for a destination folder and writes every page out as its own PDF file.</summary>
        private void SplitEveryPage()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose a folder to save the individual page files into"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var written = SplitPdfService.SplitEveryPage(_sourceFilePath!, dialog.SelectedPath);
                ShowStatus($"Saved {written.Count} page file(s) to {dialog.SelectedPath}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Split failed: {ex.Message}", isError: true);
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
