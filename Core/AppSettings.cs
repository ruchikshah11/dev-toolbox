using Newtonsoft.Json;

namespace DevToolbox.Core
{
    /// <summary>
    /// The theme choices Settings offers - System follows the Windows light/dark setting, Blue is
    /// a fixed navy-accented palette (its own look, not a Windows-following mode). Blue is
    /// appended after System (rather than inserted in natural display order) because this enum's
    /// ordinal is what's actually persisted to settings.json - reordering existing members would
    /// silently reinterpret an already-saved choice as a different mode on next launch.
    /// </summary>
    public enum AppThemeMode { Light, Dark, System, Blue }

    /// <summary>
    /// Persisted user preferences (theme, pinned tools, last/default tool, window state, search
    /// scope, and Password Generator behavior) saved as JSON under %LocalAppData% so the app
    /// reopens the way it was left instead of resetting every launch. <see cref="Load"/> caches a
    /// single shared instance so every part of the app (Theme, MainForm, SettingsForm) mutates
    /// and saves the same in-memory copy rather than two independently-loaded copies clobbering
    /// each other's changes on save.
    /// </summary>
    internal class AppSettings
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevToolbox", "settings.json");

        private static AppSettings? _cached;

        public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Light;

        // Null means "use the current theme mode's own built-in accent" - set only when the user
        // picks a custom color in Settings, so switching themes doesn't require a separate saved
        // accent per mode.
        public string? CustomAccentHtml { get; set; }

        public List<string> PinnedTools { get; set; } = new();

        // Most-recently-used first, capped at MaxRecentTools - independent of PinnedTools (a tool
        // can be both pinned and recently used; the two lists serve different purposes and neither
        // implies the other).
        public List<string> RecentTools { get; set; } = new();

        public string? LastTool { get; set; }
        public bool RememberLastTool { get; set; } = true;
        public string? DefaultTool { get; set; }
        public bool StartMaximized { get; set; }
        public int AutoClearClipboardSeconds { get; set; } = 30; // 0 = disabled
        public int PasswordHistoryLimit { get; set; } = 50; // 0 = disabled
        public int PasswordMaxLength { get; set; } = 99;
        public bool SearchIncludesDescriptions { get; set; } = true;
        public bool CategoriesExpandedByDefault { get; set; } = true;
        public List<string> CollapsedCategoryOverrides { get; set; } = new();

        /// <summary>Returns the shared settings instance, loading it from disk on first call (or falling back to defaults if there's no file yet, or it can't be read).</summary>
        public static AppSettings Load()
        {
            if (_cached is not null) return _cached;

            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(FilePath));
                    if (loaded is not null)
                    {
                        _cached = loaded;
                        return _cached;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Fall through to defaults - a corrupt or unreadable settings file shouldn't crash the app.
            }

            _cached = new AppSettings();
            return _cached;
        }

        /// <summary>Returns whether the given tool name is currently pinned.</summary>
        public bool IsPinned(string toolName) => PinnedTools.Contains(toolName);

        /// <summary>Pins or unpins the given tool name and saves immediately.</summary>
        public void TogglePinned(string toolName)
        {
            if (!PinnedTools.Remove(toolName)) PinnedTools.Add(toolName);
            Save();
        }

        /// <summary>Records the current theme choice (Light/Dark/System) and saves immediately, so the next launch starts in the same mode.</summary>
        public void SetThemeMode(AppThemeMode mode)
        {
            ThemeMode = mode;
            Save();
        }

        /// <summary>Records a custom accent color override (as an HTML hex string), or null to fall back to the current theme mode's own built-in accent, and saves immediately.</summary>
        public void SetCustomAccent(string? html)
        {
            CustomAccentHtml = html;
            Save();
        }

        // How many distinct tools RecentTools keeps - enough to be a useful "what was I just in"
        // list without turning into a second full tool index.
        private const int MaxRecentTools = 8;

        /// <summary>Moves the given tool to the front of the recently-used list (adding it if new), trims to MaxRecentTools, and saves immediately.</summary>
        public void RecordRecentTool(string toolName)
        {
            RecentTools.Remove(toolName);
            RecentTools.Insert(0, toolName);
            if (RecentTools.Count > MaxRecentTools)
            {
                RecentTools.RemoveRange(MaxRecentTools, RecentTools.Count - MaxRecentTools);
            }
            Save();
        }

        /// <summary>Records the last-opened tool's name and saves immediately, so the next launch reopens to it instead of always starting at the first tool in the list.</summary>
        public void SetLastTool(string toolName)
        {
            LastTool = toolName;
            Save();
        }

        /// <summary>Turns the "remember last-opened tool" behavior on or off and saves immediately.</summary>
        public void SetRememberLastTool(bool remember)
        {
            RememberLastTool = remember;
            Save();
        }

        /// <summary>Records which tool should open when there's no remembered tool to fall back to (null means "just use the first tool in the list") and saves immediately.</summary>
        public void SetDefaultTool(string? toolName)
        {
            DefaultTool = toolName;
            Save();
        }

        /// <summary>Turns "always open maximized" on or off and saves immediately.</summary>
        public void SetStartMaximized(bool startMaximized)
        {
            StartMaximized = startMaximized;
            Save();
        }

        /// <summary>Records how many seconds after copying a password/passphrase the clipboard should auto-clear (0 disables it) and saves immediately.</summary>
        public void SetAutoClearClipboardSeconds(int seconds)
        {
            AutoClearClipboardSeconds = seconds;
            Save();
        }

        /// <summary>Records the max number of Password Generator history entries to keep (0 disables history entirely) and saves immediately.</summary>
        public void SetPasswordHistoryLimit(int limit)
        {
            PasswordHistoryLimit = limit;
            Save();
        }

        /// <summary>Records the Password Generator length slider's upper bound and saves immediately.</summary>
        public void SetPasswordMaxLength(int maxLength)
        {
            PasswordMaxLength = maxLength;
            Save();
        }

        /// <summary>Turns "Ctrl+K search also matches tool descriptions" on or off and saves immediately.</summary>
        public void SetSearchIncludesDescriptions(bool includeDescriptions)
        {
            SearchIncludesDescriptions = includeDescriptions;
            Save();
        }

        /// <summary>Returns whether the given nav category should currently render collapsed, combining the global default with any individual override for that category.</summary>
        public bool IsCategoryCollapsed(string category)
        {
            var defaultCollapsed = !CategoriesExpandedByDefault;
            var isOverridden = CollapsedCategoryOverrides.Contains(category);
            return isOverridden ? !defaultCollapsed : defaultCollapsed;
        }

        /// <summary>Toggles the given category's collapsed state away from (or back to) the global default and saves immediately.</summary>
        public void ToggleCategoryCollapsed(string category)
        {
            if (!CollapsedCategoryOverrides.Remove(category)) CollapsedCategoryOverrides.Add(category);
            Save();
        }

        /// <summary>
        /// Changes whether categories are expanded or collapsed by default and saves immediately.
        /// Clears every individual override - each one is relative to the previous default, so
        /// leaving them in place after the default itself changes would silently invert their
        /// meaning (a category deliberately collapsed against "expanded by default" would become
        /// deliberately expanded against a new "collapsed by default").
        /// </summary>
        public void SetCategoriesExpandedByDefault(bool expanded)
        {
            CategoriesExpandedByDefault = expanded;
            CollapsedCategoryOverrides = new List<string>();
            Save();
        }

        /// <summary>
        /// Restores every setting to its factory default and saves immediately. Deliberately
        /// leaves the Password Generator's saved history file untouched - "reset settings" should
        /// restore preferences, not delete data the user might still want.
        /// </summary>
        public void ResetToDefaults()
        {
            ThemeMode = AppThemeMode.Light;
            CustomAccentHtml = null;
            PinnedTools = new List<string>();
            RecentTools = new List<string>();
            LastTool = null;
            RememberLastTool = true;
            DefaultTool = null;
            StartMaximized = false;
            AutoClearClipboardSeconds = 30;
            PasswordHistoryLimit = 50;
            PasswordMaxLength = 99;
            SearchIncludesDescriptions = true;
            CategoriesExpandedByDefault = true;
            CollapsedCategoryOverrides = new List<string>();
            Save();
        }

        /// <summary>
        /// Writes the current settings to an arbitrary file as indented JSON, so they can be
        /// copied to another machine. Unlike the normal auto-save, failures are deliberately left
        /// to propagate (IOException/UnauthorizedAccessException) - the caller (Settings dialog)
        /// shows the failure to the user rather than this silently no-op'ing.
        /// </summary>
        public void ExportTo(string path) =>
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));

        /// <summary>
        /// Reads settings from a previously-exported file and applies every field onto the live
        /// shared instance, then saves. Copies field-by-field rather than swapping out Load()'s
        /// cached reference, since Theme/MainForm/SettingsForm each already hold that same shared
        /// instance - replacing it here would leave their copies stale. Throws on a missing,
        /// unreadable, or corrupt file; the caller shows the failure to the user.
        /// </summary>
        public static void ImportFrom(string path)
        {
            var imported = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(path))
                ?? throw new JsonException("The selected file doesn't contain valid DevToolbox settings.");

            var current = Load();
            current.ThemeMode = imported.ThemeMode;
            current.CustomAccentHtml = imported.CustomAccentHtml;
            current.PinnedTools = imported.PinnedTools;
            current.RecentTools = imported.RecentTools;
            current.LastTool = imported.LastTool;
            current.RememberLastTool = imported.RememberLastTool;
            current.DefaultTool = imported.DefaultTool;
            current.StartMaximized = imported.StartMaximized;
            current.AutoClearClipboardSeconds = imported.AutoClearClipboardSeconds;
            current.PasswordHistoryLimit = imported.PasswordHistoryLimit;
            current.PasswordMaxLength = imported.PasswordMaxLength;
            current.SearchIncludesDescriptions = imported.SearchIncludesDescriptions;
            current.CategoriesExpandedByDefault = imported.CategoriesExpandedByDefault;
            current.CollapsedCategoryOverrides = imported.CollapsedCategoryOverrides;
            current.Save();
        }

        /// <summary>Best-effort write to disk - silently ignored if the settings folder isn't writable, since losing a preference save isn't worth crashing over.</summary>
        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(this));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best-effort only - worst case, this preference just doesn't persist across restarts.
            }
        }
    }
}
