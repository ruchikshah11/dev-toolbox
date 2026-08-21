using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.PdfWatermark
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick a PDF, choose Add or Remove
    /// mode, fill in that mode's fields, then save the result via a Save As dialog. See
    /// <see cref="PdfWatermarkService"/> for exactly what Remove mode can and can't find.
    /// </summary>
    public class PdfWatermarkControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();

        private readonly RadioButton _radAdd = new();
        private readonly RadioButton _radRemove = new();

        private readonly Label _lblAddText = new();
        private readonly TextBox _txtAddText = new();
        private readonly Label _lblOpacity = new();
        private readonly NumericUpDown _numOpacity = new();
        private readonly Label _lblRotation = new();
        private readonly NumericUpDown _numRotation = new();

        private readonly Label _lblRemoveText = new();
        private readonly TextBox _txtRemoveText = new();
        private readonly Label _lblRemoveNote = new();

        private readonly Button _btnRun = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;

        /// <summary>Builds the file-picker card and the mode/options/save card beneath it.</summary>
        public PdfWatermarkControl()
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

        /// <summary>Builds the Add/Remove mode toggle, both modes' fields, the run button, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "WATERMARK & SAVE", 400);

            _radAdd.Text = "Add watermark";
            _radAdd.Checked = true;
            _radAdd.AutoSize = true;
            _radAdd.Font = Theme.BaseFont;
            _radAdd.ForeColor = Theme.Text;
            _radAdd.BackColor = Theme.Card;
            _radAdd.Location = new Point(18, 44);
            _radAdd.CheckedChanged += (_, _) => UpdateModeVisibility();
            card.Controls.Add(_radAdd);

            _radRemove.Text = "Remove watermark (best-effort - see note below)";
            _radRemove.AutoSize = true;
            _radRemove.Font = Theme.BaseFont;
            _radRemove.ForeColor = Theme.Text;
            _radRemove.BackColor = Theme.Card;
            _radRemove.Location = new Point(160, 44);
            card.Controls.Add(_radRemove);

            // --- Add mode fields ---
            _lblAddText.Text = "Watermark text";
            _lblAddText.ForeColor = Theme.TextMuted;
            _lblAddText.BackColor = Theme.Card;
            _lblAddText.Font = Theme.BoldFont;
            _lblAddText.AutoSize = true;
            _lblAddText.Location = new Point(18, 82);
            card.Controls.Add(_lblAddText);

            _txtAddText.Text = "CONFIDENTIAL";
            _txtAddText.Location = new Point(18, 102);
            _txtAddText.Width = 320;
            _txtAddText.Font = Theme.BaseFont;
            _txtAddText.ForeColor = Theme.Text;
            _txtAddText.BackColor = Theme.Card;
            card.Controls.Add(_txtAddText);

            _lblOpacity.Text = "Opacity %";
            _lblOpacity.ForeColor = Theme.TextMuted;
            _lblOpacity.BackColor = Theme.Card;
            _lblOpacity.Font = Theme.BoldFont;
            _lblOpacity.AutoSize = true;
            _lblOpacity.Location = new Point(18, 140);
            card.Controls.Add(_lblOpacity);

            _numOpacity.Location = new Point(18, 160);
            _numOpacity.Width = 80;
            _numOpacity.Minimum = 1;
            _numOpacity.Maximum = 100;
            _numOpacity.Value = 30;
            _numOpacity.Font = Theme.BaseFont;
            card.Controls.Add(_numOpacity);

            _lblRotation.Text = "Rotation °";
            _lblRotation.ForeColor = Theme.TextMuted;
            _lblRotation.BackColor = Theme.Card;
            _lblRotation.Font = Theme.BoldFont;
            _lblRotation.AutoSize = true;
            _lblRotation.Location = new Point(130, 140);
            card.Controls.Add(_lblRotation);

            _numRotation.Location = new Point(130, 160);
            _numRotation.Width = 80;
            _numRotation.Minimum = -180;
            _numRotation.Maximum = 180;
            _numRotation.Value = -45;
            _numRotation.Font = Theme.BaseFont;
            card.Controls.Add(_numRotation);

            // --- Remove mode fields ---
            _lblRemoveText.Text = "Watermark text to search for and remove";
            _lblRemoveText.ForeColor = Theme.TextMuted;
            _lblRemoveText.BackColor = Theme.Card;
            _lblRemoveText.Font = Theme.BoldFont;
            _lblRemoveText.AutoSize = true;
            _lblRemoveText.Location = new Point(18, 82);
            card.Controls.Add(_lblRemoveText);

            _txtRemoveText.Location = new Point(18, 102);
            _txtRemoveText.Width = 320;
            _txtRemoveText.Font = Theme.BaseFont;
            _txtRemoveText.ForeColor = Theme.Text;
            _txtRemoveText.BackColor = Theme.Card;
            card.Controls.Add(_txtRemoveText);

            _lblRemoveNote.Text = "Best-effort: finds plain text drawn with a simple, non-subsetted font (including "
                + "watermarks this tool adds). Can't remove an image-based watermark or text drawn with a "
                + "subsetted/custom-encoded embedded font.";
            _lblRemoveNote.ForeColor = Theme.TextMuted;
            _lblRemoveNote.BackColor = Theme.Card;
            _lblRemoveNote.Font = Theme.BaseFont;
            _lblRemoveNote.Location = new Point(18, 140);
            _lblRemoveNote.Size = new Size(card.Width - 36, 44);
            _lblRemoveNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblRemoveNote);

            _btnRun.Text = "Run & Save As...";
            _btnRun.UseMnemonic = false;
            _btnRun.Location = new Point(18, 320);
            _btnRun.Size = new Size(160, 32);
            Theme.StylePrimaryButton(_btnRun);
            _btnRun.Click += OnRunClick;
            card.Controls.Add(_btnRun);

            _lblStatus.Location = new Point(18, 362);
            _lblStatus.Size = new Size(card.Width - 36, 34);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = Theme.BaseFont;
            _lblStatus.BackColor = Theme.Card;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.Visible = false;
            card.Controls.Add(_lblStatus);

            UpdateModeVisibility();
        }

        /// <summary>Shows only the fields relevant to the currently selected mode.</summary>
        private void UpdateModeVisibility()
        {
            var isAdd = _radAdd.Checked;

            _lblAddText.Visible = isAdd;
            _txtAddText.Visible = isAdd;
            _lblOpacity.Visible = isAdd;
            _numOpacity.Visible = isAdd;
            _lblRotation.Visible = isAdd;
            _numRotation.Visible = isAdd;

            _lblRemoveText.Visible = !isAdd;
            _txtRemoveText.Visible = !isAdd;
            _lblRemoveNote.Visible = !isAdd;

            _btnRun.Text = isAdd ? "Add Watermark & Save As..." : "Remove Watermark & Save As...";
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

        /// <summary>Runs whichever mode is selected, validating its fields first, then prompts for a Save As destination.</summary>
        private void OnRunClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            if (_radAdd.Checked)
            {
                RunAdd();
            }
            else
            {
                RunRemove();
            }
        }

        /// <summary>Validates the watermark text and stamps it onto every page via a Save As dialog.</summary>
        private void RunAdd()
        {
            if (string.IsNullOrWhiteSpace(_txtAddText.Text))
            {
                ShowStatus("Enter the watermark text first.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save watermarked PDF as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + "-watermarked.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                PdfWatermarkService.AddWatermark(_sourceFilePath!, dialog.FileName, _txtAddText.Text,
                    (int)_numOpacity.Value, (double)_numRotation.Value);
                ShowStatus($"Saved watermarked PDF to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Adding watermark failed: {ex.Message}", isError: true);
            }
        }

        /// <summary>Validates the search text and runs best-effort removal via a Save As dialog.</summary>
        private void RunRemove()
        {
            if (string.IsNullOrWhiteSpace(_txtRemoveText.Text))
            {
                ShowStatus("Enter the watermark text to search for first.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save cleaned PDF as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + "-unwatermarked.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var removedCount = PdfWatermarkService.RemoveWatermarkText(_sourceFilePath!, dialog.FileName, _txtRemoveText.Text);
                if (removedCount == 0)
                {
                    ShowStatus("Saved a copy, but no matching watermark text was found to remove - see the note above about what this can find.", isError: true);
                }
                else
                {
                    ShowStatus($"Removed {removedCount} matching text occurrence(s) and saved to {dialog.FileName}", isError: false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Watermark removal failed: {ex.Message}", isError: true);
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
