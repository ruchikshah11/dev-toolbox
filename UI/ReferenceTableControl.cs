namespace DevToolbox.UI
{
    /// <summary>
    /// Searchable, read-only reference table shared by the "Web Resources" lookup tools
    /// (MIME types, HTML entities, locale/language codes, ...). Filtering is a simple
    /// case-insensitive substring match across every column, applied live as you type.
    /// </summary>
    public class ReferenceTableControl : UserControl
    {
        private readonly TextBox _txtSearch = new();
        private readonly DataGridView _grid = new();
        private string[][] _allRows;
        private readonly Label _lblCount = new();
        private readonly Button _btnCopyCell = new();
        private readonly Label _lblCopied = new();
        private readonly System.Windows.Forms.Timer _copiedTimer = new() { Interval = 1200 };

        public ReferenceTableControl(string title, string[] columnHeaders, IEnumerable<string[]> rows)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            _allRows = rows.ToArray();

            var card = CardPanel.Add(this, title, 0, fill: true);

            CardPanel.AddFieldLabel(card, "Search", 18, 42);
            _txtSearch.Location = new Point(18, 62);
            _txtSearch.Width = 320;
            _txtSearch.Font = Theme.BaseFont;
            _txtSearch.TextChanged += (_, _) => ApplyFilter();
            card.Controls.Add(_txtSearch);

            _lblCount.Location = new Point(350, 66);
            _lblCount.AutoSize = true;
            _lblCount.ForeColor = Theme.TextMuted;
            _lblCount.Font = Theme.BaseFont;
            card.Controls.Add(_lblCount);

            // Double-clicking a cell is the fast path for grabbing one value (e.g. just the
            // endpoint URL, not the whole row); this button is the discoverable equivalent for
            // whichever cell is currently selected/focused.
            _btnCopyCell.Text = "Copy Selected Cell";
            _btnCopyCell.Size = new Size(150, 28);
            Theme.StyleSecondaryButton(_btnCopyCell);
            _btnCopyCell.Click += (_, _) => CopyCurrentCell();
            card.Controls.Add(_btnCopyCell);

            void PositionCopyButton() => _btnCopyCell.Location = new Point(card.Width - 18 - _btnCopyCell.Width, 60);
            card.Resize += (_, _) => PositionCopyButton();
            PositionCopyButton();

            _lblCopied.AutoSize = true;
            _lblCopied.ForeColor = Theme.Success;
            _lblCopied.Font = Theme.BoldFont;
            _lblCopied.Text = "Copied!";
            _lblCopied.Visible = false;
            card.Controls.Add(_lblCopied);
            void PositionCopiedLabel() => _lblCopied.Location = new Point(card.Width - 18 - _btnCopyCell.Width - 10 - _lblCopied.Width, 66);
            card.Resize += (_, _) => PositionCopiedLabel();
            PositionCopiedLabel();

            _copiedTimer.Tick += (_, _) =>
            {
                _lblCopied.Visible = false;
                _copiedTimer.Stop();
            };

            _grid.Location = new Point(18, 98);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _grid.Width = card.Width - 36;
            _grid.Height = card.Height - 116;
            _grid.AutoGenerateColumns = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.BackgroundColor = Theme.Card;
            _grid.BorderStyle = BorderStyle.None;
            _grid.GridColor = Theme.Border;
            _grid.EnableHeadersVisualStyles = false;
            _grid.RowTemplate.Height = 26;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.ColumnHeadersHeight = 32;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Font = Theme.MonoFont;

            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Background,
                ForeColor = Theme.TextMuted,
                Font = Theme.BoldFont,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            _grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Card,
                ForeColor = Theme.Text,
                Font = Theme.MonoFont,
                SelectionBackColor = Theme.AccentSoft,
                SelectionForeColor = Theme.Text,
                Padding = new Padding(2)
            };
            // Both colors set explicitly (not just BackColor) - leaving ForeColor unset relies on
            // it cascading from DefaultCellStyle, which doesn't reliably happen for alternating
            // rows, and hardcoded light hex values (as this used to be) never adapt to dark mode
            // at all, producing a washed-out, barely-legible stripe - see the bug this fixed.
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Background,
                ForeColor = Theme.Text
            };

            foreach (var header in columnHeaders)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header });
            }
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                CopyCellValue(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
            };
            card.Controls.Add(_grid);

            ApplyFilter();
        }

        /// <summary>Replaces the table's data (e.g. after the source values were recomputed) and reapplies the current search.</summary>
        public void SetRows(IEnumerable<string[]> rows)
        {
            _allRows = rows.ToArray();
            ApplyFilter();
        }

        private void CopyCurrentCell() => CopyCellValue(_grid.CurrentCell?.Value);

        private void CopyCellValue(object? value)
        {
            var text = value?.ToString();
            if (string.IsNullOrEmpty(text)) return;

            Clipboard.SetText(text);
            _lblCopied.Visible = true;
            _copiedTimer.Stop();
            _copiedTimer.Start();
        }

        private void ApplyFilter()
        {
            var query = _txtSearch.Text.Trim();
            _grid.Rows.Clear();

            var matches = query.Length == 0
                ? _allRows
                : _allRows.Where(row => row.Any(cell => cell.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();

            foreach (var row in matches)
            {
                _grid.Rows.Add(row);
            }
            _lblCount.Text = $"{matches.Length} of {_allRows.Length}";
        }
    }
}
