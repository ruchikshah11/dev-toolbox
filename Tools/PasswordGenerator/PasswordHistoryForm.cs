using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.PasswordGenerator
{
    /// <summary>Shows the DPAPI-encrypted history of generated passwords/passphrases, newest first - double-click a row to copy its value, or clear the whole history.</summary>
    public class PasswordHistoryForm : Form
    {
        private readonly DataGridView _grid = new();
        private readonly TextBox _txtSearch = new();
        private readonly Label _lblEmpty = new();
        private readonly Button _btnClear = new();
        private readonly Button _btnClose = new();

        // The unfiltered set loaded from the store - _grid only ever shows whatever ApplyFilter
        // decides matches _txtSearch, same live substring-across-all-columns filter every other
        // table in the app (ReferenceTableControl) already has.
        private List<PasswordHistoryEntry> _allEntries = new();

        /// <summary>Builds the dialog and loads the current history into the grid.</summary>
        public PasswordHistoryForm()
        {
            Text = "Generator History";
            Width = 560;
            Height = 420;
            MinimumSize = new Size(420, 300);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            Font = Theme.BaseFont;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Fall back to the default form icon if the exe's embedded icon can't be read.
            }

            BuildGrid();
            BuildButtons();

            LoadHistory();
        }

        /// <summary>Builds the search box, the read-only history grid (Value/Type/Generated columns), and the "no history yet" placeholder label.</summary>
        private void BuildGrid()
        {
            var lblSearch = new Label
            {
                Text = "Search",
                ForeColor = Theme.TextMuted,
                Font = Theme.BoldFont,
                AutoSize = true,
                Location = new Point(20, 4)
            };
            Controls.Add(lblSearch);

            _txtSearch.Location = new Point(20, 20);
            _txtSearch.Width = 240;
            _txtSearch.Font = Theme.BaseFont;
            _txtSearch.TextChanged += (_, _) => ApplyFilter();
            Controls.Add(_txtSearch);

            _grid.Location = new Point(20, 52);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _grid.Size = new Size(ClientSize.Width - 40, ClientSize.Height - 112);
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
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Background,
                ForeColor = Theme.Text
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", FillWeight = 15 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Generated", FillWeight = 30 });

            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var value = _grid.Rows[e.RowIndex].Cells[0].Value?.ToString();
                if (string.IsNullOrEmpty(value)) return;
                Clipboard.SetText(value);
                ClipboardAutoClear.ScheduleClear(value!);
            };
            Controls.Add(_grid);

            _lblEmpty.Text = "No history yet - generated values will appear here.";
            _lblEmpty.ForeColor = Theme.TextMuted;
            _lblEmpty.Font = Theme.BaseFont;
            _lblEmpty.AutoSize = true;
            _lblEmpty.Location = new Point(20, 56);
            _lblEmpty.Visible = false;
            Controls.Add(_lblEmpty);
        }

        /// <summary>Builds the Clear History and Close buttons.</summary>
        private void BuildButtons()
        {
            _btnClear.Text = "Clear History";
            _btnClear.Size = new Size(120, 30);
            Theme.StyleSecondaryButton(_btnClear);
            _btnClear.Click += (_, _) =>
            {
                PasswordHistoryStore.Clear();
                LoadHistory();
            };
            Controls.Add(_btnClear);

            _btnClose.Text = "Close";
            _btnClose.Size = new Size(90, 30);
            Theme.StyleSecondaryButton(_btnClose);
            _btnClose.Click += (_, _) => Close();
            Controls.Add(_btnClose);

            Resize += (_, _) => PositionButtons();
            PositionButtons();
        }

        /// <summary>Keeps Close/Clear History pinned to the bottom-right corner as the dialog is resized.</summary>
        private void PositionButtons()
        {
            _btnClose.Location = new Point(ClientSize.Width - 20 - _btnClose.Width, ClientSize.Height - 20 - _btnClose.Height);
            _btnClear.Location = new Point(_btnClose.Left - 10 - _btnClear.Width, _btnClose.Top);
        }

        /// <summary>Reloads the full entry set from the store and reapplies the current search.</summary>
        private void LoadHistory()
        {
            _allEntries = PasswordHistoryStore.Load();
            ApplyFilter();
        }

        /// <summary>Live substring filter across Value/Type/Generated - same pattern every other table in the app (ReferenceTableControl) already uses.</summary>
        private void ApplyFilter()
        {
            var query = _txtSearch.Text.Trim();
            _grid.Rows.Clear();

            var formatted = _allEntries
                .Select(entry => (entry.Value, entry.Type, Generated: entry.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")))
                .ToList();

            var matches = query.Length == 0
                ? formatted
                : formatted.Where(row =>
                    row.Value.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    row.Type.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    row.Generated.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var row in matches)
            {
                _grid.Rows.Add(row.Value, row.Type, row.Generated);
            }

            _lblEmpty.Visible = _allEntries.Count == 0;
            _grid.Visible = _allEntries.Count > 0;
        }
    }
}
