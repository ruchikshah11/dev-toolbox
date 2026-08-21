using DevToolbox.Core;

namespace DevToolbox.UI
{
    /// <summary>
    /// App-level preferences dialog: theme (Light/Dark/System), navigation behavior
    /// (remember/default tool, start maximized, search scope, sidebar category defaults),
    /// Password Generator behavior (clipboard auto-clear, history limit, max length), and a
    /// Reset to Defaults action. Content lives in a scrollable panel rather than a fixed-height
    /// layout, so adding one more setting in the future never requires re-tuning every other
    /// control's Y position to avoid overflowing the dialog.
    /// </summary>
    public class SettingsForm : Form
    {
        private const int ContentX = 20;
        private const int ContentWidth = 400;

        private readonly Panel _scroll = new();
        private readonly Panel _bottomBar = new();

        private readonly Label _lblTitle = new();

        private readonly ComboBox _cboThemeMode = new();
        private readonly Button _btnAccentSwatch = new();
        private readonly Button _btnAccentReset = new();
        private readonly CheckBox _chkRememberLastTool = new();
        private readonly ComboBox _cboDefaultTool = new();
        private readonly CheckBox _chkStartMaximized = new();
        private readonly CheckBox _chkSearchDescriptions = new();
        private readonly CheckBox _chkExpandCategories = new();

        private readonly NumericUpDown _numAutoClear = new();
        private readonly NumericUpDown _numHistoryLimit = new();
        private readonly NumericUpDown _numMaxLength = new();

        private readonly Button _btnResetDefaults = new();
        private readonly Button _btnClose = new();

        private readonly Button _btnExportSettings = new();
        private readonly Button _btnImportSettings = new();

        // (key, description) label pairs for the Keyboard Shortcuts section - kept separate from
        // _themedLabels since they need different resting colors (bold "key" vs. muted
        // description) than that list's single hint-vs-normal heuristic can express.
        private readonly List<(Label Key, Label Description)> _shortcutRows = new();

        // Every label this dialog creates that should re-color on a theme change - collected as
        // they're built (see AddHeading/AddHint) rather than named individually, since there are
        // now more hint labels than makes sense to track one field per control.
        private readonly List<Label> _themedLabels = new();
        private readonly List<CheckBox> _themedCheckBoxes = new();

        // First combo entry represents "no explicit default - just use the first tool in the
        // list", mapped back to a null AppSettings.DefaultTool.
        private const string UseFirstToolOption = "(First tool in the list)";

        // Display order for the theme dropdown - deliberately independent of AppThemeMode's own
        // ordinal (see the comment where this is used in BuildContent).
        private static readonly (AppThemeMode Mode, string Label)[] ThemeOptions =
        {
            (AppThemeMode.Light, "Light Mode"),
            (AppThemeMode.Dark, "Dark Mode"),
            (AppThemeMode.Blue, "Blue Mode"),
            (AppThemeMode.System, "System Mode"),
        };

        private readonly AppSettings _settings = AppSettings.Load();
        private readonly Action _onSettingsChanged;

        // Running layout cursor for the scrollable content - each Add*/Build* helper advances
        // this rather than every control needing a hand-picked absolute Y.
        private int _y = 20;

        /// <summary>Builds the dialog against the shared AppSettings instance, so changing any control saves immediately. Calls <paramref name="onSettingsChanged"/> after a theme or Reset to Defaults change so the owning MainForm can rebuild its shell.</summary>
        public SettingsForm(Action onSettingsChanged)
        {
            _onSettingsChanged = onSettingsChanged;

            Text = "Settings";
            Width = 460;
            Height = 640;
            MinimumSize = new Size(380, 400);
            StartPosition = FormStartPosition.CenterParent;
            Font = Theme.BaseFont;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Fall back to the default form icon if the exe's embedded icon can't be read.
            }

            // Dock=Fill must be added before the Dock=Bottom bar below it - see the docking
            // order note in MainForm/JsonFormatterControl.
            _scroll.Dock = DockStyle.Fill;
            _scroll.AutoScroll = true;
            Controls.Add(_scroll);

            BuildContent();
            BuildBottomBar();

            ApplyTheme();
        }

        /// <summary>Builds every setting control, in order, into the scrollable content panel.</summary>
        private void BuildContent()
        {
            AddHeading("Settings", big: true);

            AddLabel("Theme");
            _cboThemeMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboThemeMode.Font = Theme.BaseFont;
            _cboThemeMode.Width = ContentWidth;
            // Display order (Light, Dark, Blue, System) is independent of AppThemeMode's own
            // declaration order - Blue was appended after System in the enum to keep already-
            // persisted ordinal values meaning what they always meant, but that's an implementation
            // detail that shouldn't dictate where "Blue Mode" shows up in the dropdown. ThemeOptions
            // maps between the two, so SelectedIndex is translated through it rather than cast
            // straight to the enum.
            foreach (var option in ThemeOptions) _cboThemeMode.Items.Add(option.Label);
            _cboThemeMode.SelectedIndex = Array.FindIndex(ThemeOptions, o => o.Mode == _settings.ThemeMode);
            _cboThemeMode.SelectedIndexChanged += (_, _) =>
            {
                Theme.SetMode(ThemeOptions[_cboThemeMode.SelectedIndex].Mode);
                _onSettingsChanged();
                ApplyTheme();
            };
            AddControl(_cboThemeMode, 24);
            AddHint("Light, Dark, Blue, or follow Windows' own light/dark app setting.");

            AddLabel("Accent Color");
            BuildAccentColorRow();
            AddHint("Overrides the current theme's built-in accent everywhere it's used (buttons, links, highlights). Reset restores whichever accent the selected theme normally uses.");

            _chkRememberLastTool.Text = "Remember last-opened tool";
            _chkRememberLastTool.Checked = _settings.RememberLastTool;
            _chkRememberLastTool.CheckedChanged += (_, _) => _settings.SetRememberLastTool(_chkRememberLastTool.Checked);
            AddCheckbox(_chkRememberLastTool);
            AddHint("When on, the app reopens to whichever tool you last had selected. When off, it always uses the default landing tool below.");

            AddLabel("Default landing tool");
            _cboDefaultTool.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboDefaultTool.Font = Theme.BaseFont;
            _cboDefaultTool.Width = ContentWidth;
            _cboDefaultTool.Items.Add(UseFirstToolOption);
            foreach (var tool in ToolRegistry.All) _cboDefaultTool.Items.Add(tool.Name);
            var selectedIndex = _settings.DefaultTool is null ? 0 : _cboDefaultTool.Items.IndexOf(_settings.DefaultTool);
            _cboDefaultTool.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            _cboDefaultTool.SelectedIndexChanged += (_, _) =>
            {
                var selected = _cboDefaultTool.SelectedItem as string;
                _settings.SetDefaultTool(selected == UseFirstToolOption ? null : selected);
            };
            AddControl(_cboDefaultTool, 24);
            AddHint("Used on first launch, or whenever there's no remembered tool to reopen.");

            _chkStartMaximized.Text = "Start maximized";
            _chkStartMaximized.Checked = _settings.StartMaximized;
            _chkStartMaximized.CheckedChanged += (_, _) => _settings.SetStartMaximized(_chkStartMaximized.Checked);
            AddCheckbox(_chkStartMaximized);
            AddHint("When on, the app window opens maximized instead of its normal remembered size.");

            _chkSearchDescriptions.Text = "Ctrl+K search also matches tool descriptions";
            _chkSearchDescriptions.Checked = _settings.SearchIncludesDescriptions;
            _chkSearchDescriptions.CheckedChanged += (_, _) => _settings.SetSearchIncludesDescriptions(_chkSearchDescriptions.Checked);
            AddCheckbox(_chkSearchDescriptions);
            AddHint("When on, sidebar search also matches text in each tool's description, not just its name.");

            _chkExpandCategories.Text = "Expand categories by default in the sidebar";
            _chkExpandCategories.Checked = _settings.CategoriesExpandedByDefault;
            _chkExpandCategories.CheckedChanged += (_, _) =>
            {
                _settings.SetCategoriesExpandedByDefault(_chkExpandCategories.Checked);
                _onSettingsChanged();
            };
            AddCheckbox(_chkExpandCategories);
            AddHint("Click any category header in the sidebar to collapse or expand it - this sets the starting state for all of them.");

            _y += 16;
            AddHeading("PASSWORD GENERATOR");

            AddLabel("Auto-clear clipboard after copying (seconds, 0 = off)");
            _numAutoClear.Minimum = 0;
            _numAutoClear.Maximum = 300;
            _numAutoClear.Value = ClampToRange(_settings.AutoClearClipboardSeconds, 0, 300);
            _numAutoClear.Font = Theme.BaseFont;
            _numAutoClear.Width = 80;
            _numAutoClear.ValueChanged += (_, _) => _settings.SetAutoClearClipboardSeconds((int)_numAutoClear.Value);
            AddControl(_numAutoClear, 24);
            AddHint("Clears the clipboard this many seconds after copying a password/passphrase, so it doesn't linger there indefinitely.");

            AddLabel("Password history limit (0 = off)");
            _numHistoryLimit.Minimum = 0;
            _numHistoryLimit.Maximum = 500;
            _numHistoryLimit.Value = ClampToRange(_settings.PasswordHistoryLimit, 0, 500);
            _numHistoryLimit.Font = Theme.BaseFont;
            _numHistoryLimit.Width = 80;
            _numHistoryLimit.ValueChanged += (_, _) => _settings.SetPasswordHistoryLimit((int)_numHistoryLimit.Value);
            AddControl(_numHistoryLimit, 24);
            AddHint("How many recent Password Generator values to keep in History - set to 0 to turn history off entirely.");

            AddLabel("Password max length (20-128)");
            _numMaxLength.Minimum = 20;
            _numMaxLength.Maximum = 128;
            _numMaxLength.Value = ClampToRange(_settings.PasswordMaxLength, 20, 128);
            _numMaxLength.Font = Theme.BaseFont;
            _numMaxLength.Width = 80;
            _numMaxLength.ValueChanged += (_, _) => _settings.SetPasswordMaxLength((int)_numMaxLength.Value);
            AddControl(_numMaxLength, 24);
            AddHint("Upper bound of the length slider in Password Generator's Password mode.");

            _y += 16;
            AddHeading("KEYBOARD SHORTCUTS");
            AddShortcutRow("Ctrl+K", "Focus the sidebar search box");
            AddShortcutRow("Esc", "Clear the sidebar search filter");
            AddShortcutRow("Ctrl+,", "Open Settings");
            _y += 8;

            _y += 16;
            AddHeading("BACKUP");
            BuildExportImportRow();
            AddHint("Export saves every setting on this page (theme/accent, pinned and recently-used tools, navigation and Password Generator options) to a file. Import applies a previously-exported file, overwriting your current settings.");

            _y += 8;
        }

        /// <summary>Builds the accent color swatch (opens a ColorDialog) and its "reset to theme default" button, side by side.</summary>
        private void BuildAccentColorRow()
        {
            _btnAccentSwatch.Size = new Size(50, 28);
            _btnAccentSwatch.FlatStyle = FlatStyle.Flat;
            _btnAccentSwatch.FlatAppearance.BorderSize = 1;
            _btnAccentSwatch.Cursor = Cursors.Hand;
            _btnAccentSwatch.Location = new Point(ContentX, _y);
            _btnAccentSwatch.Click += (_, _) => PickAccentColor();
            _scroll.Controls.Add(_btnAccentSwatch);

            _btnAccentReset.Text = "Reset to Theme Default";
            _btnAccentReset.Size = new Size(190, 28);
            _btnAccentReset.Location = new Point(ContentX + _btnAccentSwatch.Width + 10, _y);
            Theme.StyleSecondaryButton(_btnAccentReset);
            _btnAccentReset.Click += (_, _) =>
            {
                Theme.SetCustomAccent(null);
                _onSettingsChanged();
                ApplyTheme();
            };
            _scroll.Controls.Add(_btnAccentReset);

            _y += _btnAccentSwatch.Height + 4;
        }

        /// <summary>Opens a ColorDialog seeded with the current accent, and applies whatever's picked as a custom override.</summary>
        private void PickAccentColor()
        {
            using var dialog = new ColorDialog { Color = Theme.Accent, FullOpen = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            Theme.SetCustomAccent(dialog.Color);
            _onSettingsChanged();
            ApplyTheme();
        }

        /// <summary>Adds one read-only "key combo -> what it does" row and advances the layout cursor.</summary>
        private void AddShortcutRow(string keys, string description)
        {
            var keyLabel = new Label
            {
                Text = keys,
                UseMnemonic = false,
                Font = Theme.BoldFont,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(70, 20),
                Location = new Point(ContentX, _y)
            };
            _scroll.Controls.Add(keyLabel);

            var descriptionLabel = new Label
            {
                Text = description,
                UseMnemonic = false,
                Font = Theme.BaseFont,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(ContentWidth - 70, 20),
                Location = new Point(ContentX + 70, _y)
            };
            _scroll.Controls.Add(descriptionLabel);

            _shortcutRows.Add((keyLabel, descriptionLabel));
            _y += 24;
        }

        /// <summary>Builds the Export/Import Settings buttons, side by side.</summary>
        private void BuildExportImportRow()
        {
            _btnExportSettings.Text = "Export Settings...";
            _btnExportSettings.Size = new Size(190, 30);
            _btnExportSettings.Location = new Point(ContentX, _y);
            Theme.StyleSecondaryButton(_btnExportSettings);
            _btnExportSettings.Click += (_, _) => ExportSettings();
            _scroll.Controls.Add(_btnExportSettings);

            _btnImportSettings.Text = "Import Settings...";
            _btnImportSettings.Size = new Size(190, 30);
            _btnImportSettings.Location = new Point(ContentX + _btnExportSettings.Width + 10, _y);
            Theme.StyleSecondaryButton(_btnImportSettings);
            _btnImportSettings.Click += (_, _) => ImportSettings();
            _scroll.Controls.Add(_btnImportSettings);

            _y += _btnExportSettings.Height + 4;
        }

        /// <summary>Prompts for a destination file and writes every current setting to it as JSON.</summary>
        private void ExportSettings()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export DevToolbox settings",
                FileName = "DevToolbox-settings.json",
                Filter = "JSON files (*.json)|*.json"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _settings.ExportTo(dialog.FileName);
                MessageBox.Show(this, $"Settings exported to {dialog.FileName}", "Export Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"Couldn't export settings: {ex.Message}", "Export Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Prompts for a previously-exported file, confirms (since this overwrites every current setting), then applies it.</summary>
        private void ImportSettings()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import DevToolbox settings",
                Filter = "JSON files (*.json)|*.json"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var confirm = MessageBox.Show(
                this,
                "Import settings from this file? This overwrites every setting on this page with the imported values.",
                "Import Settings",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                AppSettings.ImportFrom(dialog.FileName);
                Theme.SetMode(_settings.ThemeMode);
                RefreshControlsFromSettings();
                _onSettingsChanged();
                ApplyTheme();
                MessageBox.Show(this, "Settings imported.", "Import Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Newtonsoft.Json.JsonException)
            {
                MessageBox.Show(this, $"Couldn't import settings: {ex.Message}", "Import Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Adds a heading label (either the dialog's big title, or a smaller section header) and advances the layout cursor.</summary>
        private void AddHeading(string text, bool big = false)
        {
            var label = new Label
            {
                Text = text,
                Font = big ? new Font("Segoe UI Semibold", 14f) : Theme.SectionFont,
                AutoSize = true,
                Location = new Point(ContentX, _y)
            };
            _scroll.Controls.Add(label);
            _themedLabels.Add(label);
            _y += label.PreferredHeight + (big ? 24 : 14);
        }

        /// <summary>Adds a bold field-name label (above a dropdown/numeric control) and advances the layout cursor.</summary>
        private void AddLabel(string text)
        {
            var label = new Label
            {
                Text = text,
                Font = Theme.BoldFont,
                AutoSize = true,
                MaximumSize = new Size(ContentWidth, 0),
                Location = new Point(ContentX, _y)
            };
            _scroll.Controls.Add(label);
            _themedLabels.Add(label);
            _y += label.PreferredHeight + 4;
        }

        /// <summary>Places the given control at the current layout cursor and advances past it.</summary>
        private void AddControl(Control control, int height)
        {
            control.Location = new Point(ContentX, _y);
            _scroll.Controls.Add(control);
            _y += height + 4;
        }

        /// <summary>Adds a checkbox (flat-styled so its glyph re-themes correctly - see the dark-mode note this fixed) and advances the layout cursor.</summary>
        private void AddCheckbox(CheckBox checkBox)
        {
            checkBox.Font = Theme.BoldFont;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.AutoSize = true;
            checkBox.Location = new Point(ContentX, _y);
            _scroll.Controls.Add(checkBox);
            _themedCheckBoxes.Add(checkBox);
            _y += checkBox.PreferredSize.Height + 4;
        }

        /// <summary>Adds a small muted one-line explanation of whatever setting was just added, so its purpose is clear without guessing from the label alone.</summary>
        private void AddHint(string text)
        {
            var label = new Label
            {
                Text = text,
                UseMnemonic = false,
                Font = Theme.BaseFont,
                AutoSize = false,
                MaximumSize = new Size(0, 0),
                Size = new Size(ContentWidth, 0),
                Location = new Point(ContentX, _y)
            };
            // AutoSize=false with an explicit Size lets the label wrap within ContentWidth, but
            // still needs a manual height pass since it won't auto-grow like a true AutoSize
            // label would - GetPreferredSize gives the wrapped height for that fixed width.
            label.Height = label.GetPreferredSize(new Size(ContentWidth, 0)).Height;
            _scroll.Controls.Add(label);
            _themedLabels.Add(label);
            _y += label.Height + 14;
        }

        /// <summary>Clamps an int setting value into a NumericUpDown's decimal range.</summary>
        private static decimal ClampToRange(int value, decimal min, decimal max)
        {
            var d = (decimal)value;
            return d < min ? min : d > max ? max : d;
        }

        /// <summary>Builds the fixed Reset to Defaults / Close bar pinned to the bottom of the dialog.</summary>
        private void BuildBottomBar()
        {
            _bottomBar.Dock = DockStyle.Bottom;
            _bottomBar.Height = 60;
            Controls.Add(_bottomBar);

            _btnResetDefaults.Text = "Reset to Defaults";
            _btnResetDefaults.Size = new Size(150, 30);
            _btnResetDefaults.Location = new Point(20, 15);
            _btnResetDefaults.Click += (_, _) => ResetToDefaults();
            _btnResetDefaults.MouseEnter += (_, _) => { if (_btnResetDefaults.Enabled) _btnResetDefaults.BackColor = Theme.AccentSoft; };
            _btnResetDefaults.MouseLeave += (_, _) => { if (_btnResetDefaults.Enabled) _btnResetDefaults.BackColor = Theme.Card; };
            _bottomBar.Controls.Add(_btnResetDefaults);

            _btnClose.Text = "Close";
            _btnClose.Size = new Size(90, 30);
            _btnClose.Click += (_, _) => Close();
            // Reads Theme.* live on every hover rather than a value captured at subscribe time,
            // so these stay correct even after a theme change re-colors the resting state.
            _btnClose.MouseEnter += (_, _) => { if (_btnClose.Enabled) _btnClose.BackColor = Theme.AccentSoft; };
            _btnClose.MouseLeave += (_, _) => { if (_btnClose.Enabled) _btnClose.BackColor = Theme.Card; };
            _bottomBar.Controls.Add(_btnClose);

            void PositionClose() => _btnClose.Location = new Point(_bottomBar.Width - 20 - _btnClose.Width, 15);
            _bottomBar.Resize += (_, _) => PositionClose();
            PositionClose();
        }

        /// <summary>Confirms, then restores every setting to its default, refreshes every control to match, and asks the owner to rebuild.</summary>
        private void ResetToDefaults()
        {
            var confirm = MessageBox.Show(
                this,
                "Reset all settings to their defaults? This unpins any pinned tools and restores every option in this dialog. Your saved Password Generator history is not affected.",
                "Reset to Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            _settings.ResetToDefaults();
            RefreshControlsFromSettings();
            _onSettingsChanged();
            ApplyTheme();
        }

        /// <summary>Re-reads every control's displayed value from the (just-reset) settings - setting Theme mode via the combo lets its own handler apply/persist the palette rather than duplicating that logic here.</summary>
        private void RefreshControlsFromSettings()
        {
            _cboThemeMode.SelectedIndex = Array.FindIndex(ThemeOptions, o => o.Mode == _settings.ThemeMode);
            _chkRememberLastTool.Checked = _settings.RememberLastTool;

            var selectedIndex = _settings.DefaultTool is null ? 0 : _cboDefaultTool.Items.IndexOf(_settings.DefaultTool);
            _cboDefaultTool.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

            _chkStartMaximized.Checked = _settings.StartMaximized;
            _chkSearchDescriptions.Checked = _settings.SearchIncludesDescriptions;
            _chkExpandCategories.Checked = _settings.CategoriesExpandedByDefault;
            _numAutoClear.Value = ClampToRange(_settings.AutoClearClipboardSeconds, _numAutoClear.Minimum, _numAutoClear.Maximum);
            _numHistoryLimit.Value = ClampToRange(_settings.PasswordHistoryLimit, _numHistoryLimit.Minimum, _numHistoryLimit.Maximum);
            _numMaxLength.Value = ClampToRange(_settings.PasswordMaxLength, _numMaxLength.Minimum, _numMaxLength.Maximum);
        }

        /// <summary>
        /// Re-applies every control's colors from the current Theme - called once at startup and
        /// again whenever the theme changes from within this already-open dialog, since none of
        /// these controls otherwise refresh themselves after their initial construction.
        /// </summary>
        private void ApplyTheme()
        {
            BackColor = Theme.Background;
            _scroll.BackColor = Theme.Background;
            _bottomBar.BackColor = Theme.Background;

            foreach (var label in _themedLabels)
            {
                // Hints (built via AddHint) use MaximumSize as their wrap-detection marker;
                // headings/field labels are AutoSize instead, so this distinguishes the two
                // without a separate tracking list.
                var isHint = label.MaximumSize == new Size(0, 0) && !label.AutoSize;
                label.ForeColor = isHint ? Theme.TextMuted : Theme.Text;
                label.BackColor = Theme.Background;
            }

            foreach (var checkBox in _themedCheckBoxes)
            {
                checkBox.ForeColor = Theme.Text;
                checkBox.BackColor = Theme.Background;
                checkBox.FlatAppearance.BorderColor = Theme.Border;
            }

            // Theme.StyleSecondaryButton isn't reused here on purpose - calling it again on every
            // toggle would stack a duplicate MouseEnter/MouseLeave handler each time; those
            // existing handlers already re-read Theme.Card/Theme.AccentSoft live on every hover,
            // so only the resting-state colors need to be re-applied directly. This covers every
            // secondary-styled button in the dialog, not just the bottom bar's two.
            foreach (var button in new[] { _btnResetDefaults, _btnClose, _btnAccentReset, _btnExportSettings, _btnImportSettings })
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Theme.Border;
                button.BackColor = Theme.Card;
                button.ForeColor = Theme.Text;
                button.Font = Theme.BoldFont;
                button.Cursor = Cursors.Hand;
            }

            // The swatch's own BackColor *is* its content (no text/icon) - reflects a custom
            // accent if one is set, or the current theme's own built-in accent otherwise, either
            // way just by reading Theme.Accent fresh.
            _btnAccentSwatch.BackColor = Theme.Accent;
            _btnAccentSwatch.FlatAppearance.BorderColor = Theme.Border;

            foreach (var (key, description) in _shortcutRows)
            {
                key.ForeColor = Theme.Text;
                key.BackColor = Theme.Background;
                description.ForeColor = Theme.TextMuted;
                description.BackColor = Theme.Background;
            }
        }
    }
}
