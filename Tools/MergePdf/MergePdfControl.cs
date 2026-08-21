using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.MergePdf
{
    /// <summary>
    /// File-based tool (no paste box - PDF is a binary format): pick 2+ PDF files, reorder them
    /// with Move Up/Move Down (the order shown in the list is the order they'll be merged in),
    /// then merge and save the result via a Save As dialog.
    /// </summary>
    public class MergePdfControl : UserControl
    {
        private readonly Button _btnAddFiles = new();
        private readonly Button _btnRemove = new();
        private readonly Button _btnMoveUp = new();
        private readonly Button _btnMoveDown = new();
        private readonly ListBox _listFiles = new();
        private readonly Button _btnMerge = new();
        private readonly Label _lblStatus = new();

        // Full paths in merge order, parallel to _listFiles' (filename-only) display items.
        private readonly List<string> _filePaths = new();

        /// <summary>Builds the file-list-with-reorder card and the merge/save card beneath it.</summary>
        public MergePdfControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Same-edge Dock=Top siblings stack in reverse add-order (see PdfPasswordRemoverControl) -
            // the action card is added first so the source-files card (added last) ends up
            // visually on top of it.
            BuildActionCard();
            BuildSourceCard();
        }

        /// <summary>Builds the "add PDF files" card: add/remove/reorder buttons plus the ordered file list.</summary>
        private void BuildSourceCard()
        {
            var card = CardPanel.Add(this, "PDF FILES (MERGED IN THIS ORDER)", 300);

            _btnAddFiles.Text = "Add Files...";
            _btnAddFiles.UseMnemonic = false;
            _btnAddFiles.Location = new Point(18, 44);
            _btnAddFiles.Size = new Size(120, 32);
            Theme.StyleSecondaryButton(_btnAddFiles);
            _btnAddFiles.Click += OnAddFilesClick;
            card.Controls.Add(_btnAddFiles);

            _btnRemove.Text = "Remove";
            _btnRemove.UseMnemonic = false;
            _btnRemove.Location = new Point(146, 44);
            _btnRemove.Size = new Size(90, 32);
            Theme.StyleSecondaryButton(_btnRemove);
            _btnRemove.Click += OnRemoveClick;
            card.Controls.Add(_btnRemove);

            _btnMoveUp.Text = "Move Up";
            _btnMoveUp.UseMnemonic = false;
            _btnMoveUp.Location = new Point(244, 44);
            _btnMoveUp.Size = new Size(100, 32);
            Theme.StyleSecondaryButton(_btnMoveUp);
            _btnMoveUp.Click += (_, _) => MoveSelected(-1);
            card.Controls.Add(_btnMoveUp);

            _btnMoveDown.Text = "Move Down";
            _btnMoveDown.UseMnemonic = false;
            _btnMoveDown.Location = new Point(352, 44);
            _btnMoveDown.Size = new Size(100, 32);
            Theme.StyleSecondaryButton(_btnMoveDown);
            _btnMoveDown.Click += (_, _) => MoveSelected(1);
            card.Controls.Add(_btnMoveDown);

            _listFiles.Font = Theme.BaseFont;
            _listFiles.ForeColor = Theme.Text;
            _listFiles.BackColor = Theme.Card;
            _listFiles.BorderStyle = BorderStyle.FixedSingle;
            _listFiles.IntegralHeight = false;
            _listFiles.Location = new Point(18, 86);
            _listFiles.Size = new Size(card.Width - 36, card.Height - 104);
            _listFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            card.Controls.Add(_listFiles);
        }

        /// <summary>Builds the Merge & Save As button and the status label.</summary>
        private void BuildActionCard()
        {
            var card = CardPanel.Add(this, "MERGE & SAVE", 130);

            _btnMerge.Text = "Merge & Save As...";
            _btnMerge.UseMnemonic = false;
            _btnMerge.Location = new Point(18, 44);
            _btnMerge.Size = new Size(180, 32);
            Theme.StylePrimaryButton(_btnMerge);
            _btnMerge.Click += OnMergeClick;
            card.Controls.Add(_btnMerge);

            _lblStatus.Location = new Point(18, 86);
            _lblStatus.Size = new Size(card.Width - 36, 34);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = Theme.BaseFont;
            _lblStatus.BackColor = Theme.Card;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.Visible = false;
            card.Controls.Add(_lblStatus);
        }

        /// <summary>Opens a multi-select file picker and appends every chosen PDF to the ordered list.</summary>
        private void OnAddFilesClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose PDF files to merge",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            foreach (var path in dialog.FileNames)
            {
                _filePaths.Add(path);
                _listFiles.Items.Add(Path.GetFileName(path));
            }
            HideStatus();
        }

        /// <summary>Removes the currently selected file from the merge list.</summary>
        private void OnRemoveClick(object? sender, EventArgs e)
        {
            var index = _listFiles.SelectedIndex;
            if (index < 0) return;

            _filePaths.RemoveAt(index);
            _listFiles.Items.RemoveAt(index);
        }

        /// <summary>Swaps the selected item with its neighbor <paramref name="direction"/> steps away (-1 = up, +1 = down).</summary>
        private void MoveSelected(int direction)
        {
            var index = _listFiles.SelectedIndex;
            var newIndex = index + direction;
            if (index < 0 || newIndex < 0 || newIndex >= _listFiles.Items.Count) return;

            (_filePaths[index], _filePaths[newIndex]) = (_filePaths[newIndex], _filePaths[index]);

            var item = _listFiles.Items[index];
            _listFiles.Items.RemoveAt(index);
            _listFiles.Items.Insert(newIndex, item);
            _listFiles.SelectedIndex = newIndex;
        }

        /// <summary>Validates the current list, merges, and prompts for a Save As destination.</summary>
        private void OnMergeClick(object? sender, EventArgs e)
        {
            if (_filePaths.Count == 0)
            {
                ShowStatus("Add at least one PDF file first.", isError: true);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save merged PDF as",
                FileName = "merged.pdf",
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                MergePdfService.Merge(_filePaths, dialog.FileName);
                ShowStatus($"Saved merged PDF ({_filePaths.Count} files) to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Merge failed: {ex.Message}", isError: true);
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
