using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.RotatePdf
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick a PDF, choose a rotation
    /// angle and (optionally) which pages it applies to, then save the rotated copy via a Save
    /// As dialog.
    /// </summary>
    public class RotatePdfControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly ComboBox _cboDegrees = new();
        private readonly TextBox _txtPages = new();
        private readonly Button _btnRotate = new();
        private readonly Label _lblStatus = new();

        private string? _sourceFilePath;

        /// <summary>Builds the file-picker card and the rotation-options/save card beneath it.</summary>
        public RotatePdfControl()
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

        /// <summary>Builds the rotation-angle dropdown, the optional page-numbers field, the Rotate button, and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "ROTATION & SAVE", 250);

            CardPanel.AddFieldLabel(card, "Rotate by", 18, 44);
            _cboDegrees.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboDegrees.Font = Theme.BaseFont;
            _cboDegrees.Location = new Point(18, 64);
            _cboDegrees.Width = 160;
            _cboDegrees.Items.AddRange(new object[] { "90 (clockwise)", "180", "270 (counter-clockwise)" });
            _cboDegrees.SelectedIndex = 0;
            card.Controls.Add(_cboDegrees);

            CardPanel.AddFieldLabel(card, "Pages to rotate (e.g. 1,3,5-7 - leave blank for every page)", 18, 106);
            _txtPages.Location = new Point(18, 126);
            _txtPages.Width = 320;
            _txtPages.Font = Theme.BaseFont;
            _txtPages.ForeColor = Theme.Text;
            _txtPages.BackColor = Theme.Card;
            card.Controls.Add(_txtPages);

            _btnRotate.Text = "Rotate & Save As...";
            _btnRotate.UseMnemonic = false;
            _btnRotate.Location = new Point(18, 164);
            _btnRotate.Size = new Size(180, 32);
            Theme.StylePrimaryButton(_btnRotate);
            _btnRotate.Click += OnRotateClick;
            card.Controls.Add(_btnRotate);

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
                Title = "Choose a PDF file",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _sourceFilePath = dialog.FileName;
            _lblFileName.Text = Path.GetFileName(_sourceFilePath);
            HideStatus();
        }

        /// <summary>Validates the current selection and page spec, rotates, and prompts for a Save As destination.</summary>
        private void OnRotateClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a PDF file first.", isError: true);
                return;
            }

            HashSet<int>? pageNumbers;
            try
            {
                pageNumbers = RotatePdfService.ParsePageNumbers(_txtPages.Text);
            }
            catch (FormatException ex)
            {
                ShowStatus(ex.Message, isError: true);
                return;
            }

            var degrees = _cboDegrees.SelectedIndex switch
            {
                0 => 90,
                1 => 180,
                _ => 270
            };

            using var dialog = new SaveFileDialog
            {
                Title = "Save rotated PDF as",
                FileName = Path.GetFileNameWithoutExtension(_sourceFilePath) + "-rotated.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                RotatePdfService.Rotate(_sourceFilePath, degrees, pageNumbers, dialog.FileName);
                var scope = pageNumbers is { Count: > 0 } ? $"{pageNumbers.Count} page(s)" : "every page";
                ShowStatus($"Saved rotated PDF ({scope} rotated {degrees}°) to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Rotation failed: {ex.Message}", isError: true);
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
