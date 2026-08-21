using System.Drawing;
using System.Windows.Forms;
using DevToolbox.Core;
using Microsoft.Win32;

namespace DevToolbox.UI
{
    // Colors are mutable (not readonly) so ToggleMode() can swap the whole palette at runtime.
    // Every control reads these at construction time, so flipping the palette and then
    // recreating the header/nav/content controls (see MainForm.RebuildChrome) is enough to
    // re-theme the whole app - no per-control "theme changed" plumbing needed.
    internal static class Theme
    {
        public static bool IsDark { get; private set; }

        public static Color Background { get; private set; }
        public static Color Card { get; private set; }
        public static Color Border { get; private set; }
        public static Color Text { get; private set; }
        public static Color TextMuted { get; private set; }
        public static Color Accent { get; private set; }
        public static Color AccentDark { get; private set; }
        public static Color AccentSoft { get; private set; }
        public static Color Success { get; private set; }
        public static Color Error { get; private set; }
        public static Color Warning { get; private set; }

        // Muted, background-only versions of Success/Error - for tinting a whole line (e.g. a
        // diff's added/removed rows) without the vivid full-saturation color overwhelming the
        // syntax-highlighted text painted on top of it.
        public static Color SuccessSoft { get; private set; }
        public static Color ErrorSoft { get; private set; }

        // Sidebar palette - light mode matches freeformatter.com's tool navigation (white/near-
        // white background, dark category headers, blue links); dark mode inverts it.
        public static Color NavBackground { get; private set; }
        public static Color NavCategoryText { get; private set; }
        public static Color NavLinkText { get; private set; }
        public static Color NavLinkHover { get; private set; }
        public static Color NavSelectedText { get; private set; }
        public static Color NavSelectedBackground { get; private set; }

        public static readonly Font TitleFont = new("Segoe UI Semibold", 14f);
        public static readonly Font SubtitleFont = new("Segoe UI", 9f);
        public static readonly Font SectionFont = new("Segoe UI Semibold", 10.5f);
        public static readonly Font BaseFont = new("Segoe UI", 9.5f);
        public static readonly Font BoldFont = new("Segoe UI Semibold", 9.5f);
        public static readonly Font MonoFont = new("Cascadia Mono", 9.5f);

        // Shares MainForm's exact same settings instance (AppSettings.Load() caches it) rather
        // than loading its own separate copy - otherwise whichever of the two saved last would
        // silently clobber the other's in-memory changes (e.g. toggling the theme could wipe out
        // a pinned-tools change that hadn't been saved yet, or vice versa).
        private static readonly AppSettings Settings = AppSettings.Load();

        /// <summary>Applies whichever mode was saved from the last run, so the app reopens the way it was left instead of always starting light.</summary>
        static Theme() => Apply(Settings.ThemeMode);

        /// <summary>Applies the given mode's color palette - does not persist it; see <see cref="SetMode"/> for the user-facing action that does.</summary>
        private static void Apply(AppThemeMode mode)
        {
            // System is the only mode that isn't a fixed palette - it resolves to whichever of
            // Light/Dark actually matches Windows' own setting. Blue is never an auto-resolved
            // outcome of System - like Light and Dark, it's only ever reached by an explicit
            // choice in Settings.
            var resolved = mode == AppThemeMode.System
                ? (IsSystemDarkMode() ? AppThemeMode.Dark : AppThemeMode.Light)
                : mode;

            switch (resolved)
            {
                case AppThemeMode.Dark: ApplyDark(); break;
                case AppThemeMode.Blue: ApplyBlue(); break;
                default: ApplyLight(); break;
            }

            // Blue is a dark-background palette (like Dark), so it reads the same as Dark to
            // every consumer that branches on IsDark alone (e.g. JsonColors/MarkupSyntaxColors'
            // dark-friendly vs. light-friendly syntax highlight colors).
            IsDark = resolved is AppThemeMode.Dark or AppThemeMode.Blue;

            ApplyCustomAccentOverride();
        }

        /// <summary>Applies the given theme mode (resolving System against the current Windows setting), and saves the choice so the next launch starts in the same mode.</summary>
        public static void SetMode(AppThemeMode mode)
        {
            Apply(mode);
            Settings.SetThemeMode(mode);
        }

        /// <summary>Applies the current custom accent color (if any) on top of whichever palette Apply() just set, and saves it. A null/empty color clears the override, restoring the mode's own built-in accent.</summary>
        public static void SetCustomAccent(Color? color)
        {
            Settings.SetCustomAccent(color is null ? null : ColorTranslator.ToHtml(color.Value));
            ApplyCustomAccentOverride();
        }

        /// <summary>
        /// Overrides Accent/AccentDark/AccentSoft with a user-chosen color, derived the same way
        /// every built-in palette derives its own hover/tint shades from its base accent - lighten
        /// for hover on a dark background (reads as "lighting up"), darken on a light one, and a
        /// soft background tint blended toward the current mode's own Card color so it still fits
        /// whichever palette is active. A no-op if no custom color is set.
        /// </summary>
        private static void ApplyCustomAccentOverride()
        {
            if (Settings.CustomAccentHtml is not { Length: > 0 } html) return;

            Color custom;
            try
            {
                custom = ColorTranslator.FromHtml(html);
            }
            catch (Exception)
            {
                // ColorTranslator.FromHtml throws a plain Exception (not a more specific type) for
                // unparseable input - a corrupted settings.json value shouldn't crash the app, it
                // should just fall back to the current palette's own built-in accent.
                return;
            }

            Accent = custom;
            AccentDark = IsDark ? Lighten(custom, 0.25) : Darken(custom, 0.25);
            AccentSoft = Blend(Card, custom, 0.18);
        }

        private static Color Blend(Color from, Color to, double t) => Color.FromArgb(
            (int)(from.R + (to.R - from.R) * t),
            (int)(from.G + (to.G - from.G) * t),
            (int)(from.B + (to.B - from.B) * t));

        private static Color Lighten(Color c, double amount) => Blend(c, Color.White, amount);
        private static Color Darken(Color c, double amount) => Blend(c, Color.Black, amount);

        /// <summary>
        /// Reads Windows' own light/dark app theme preference from the registry
        /// (Settings > Personalization > Colors > "Choose your mode" on Windows 10/11). Falls back
        /// to light if the value is missing or unreadable (e.g. a Windows version that predates
        /// this setting).
        /// </summary>
        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int lightThemeValue && lightThemeValue == 0;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void ApplyLight()
        {
            Background = ColorTranslator.FromHtml("#F2F4F9");
            Card = Color.White;
            Border = ColorTranslator.FromHtml("#DDE1EA");
            Text = ColorTranslator.FromHtml("#1C2233");
            TextMuted = ColorTranslator.FromHtml("#6B7280");
            Accent = ColorTranslator.FromHtml("#2F6FED");
            AccentDark = ColorTranslator.FromHtml("#1F52C4");
            AccentSoft = ColorTranslator.FromHtml("#EAF1FF");
            Success = ColorTranslator.FromHtml("#16A34A");
            Error = ColorTranslator.FromHtml("#DC2626");
            Warning = ColorTranslator.FromHtml("#D97706");
            SuccessSoft = ColorTranslator.FromHtml("#E6F7EC");
            ErrorSoft = ColorTranslator.FromHtml("#FCEAEA");

            NavBackground = ColorTranslator.FromHtml("#FBFCFE");
            NavCategoryText = ColorTranslator.FromHtml("#24292F");
            NavLinkText = ColorTranslator.FromHtml("#0969DA");
            NavLinkHover = ColorTranslator.FromHtml("#0550AE");
            NavSelectedText = ColorTranslator.FromHtml("#0550AE");
            NavSelectedBackground = ColorTranslator.FromHtml("#EDF3FF");
        }

        private static void ApplyDark()
        {
            Background = ColorTranslator.FromHtml("#15171E");
            Card = ColorTranslator.FromHtml("#1E212B");
            Border = ColorTranslator.FromHtml("#343948");
            Text = ColorTranslator.FromHtml("#E7E9EE");
            TextMuted = ColorTranslator.FromHtml("#9BA3B4");
            Accent = ColorTranslator.FromHtml("#5B8DF6");
            AccentDark = ColorTranslator.FromHtml("#82A9FF");
            AccentSoft = ColorTranslator.FromHtml("#25324D");
            Success = ColorTranslator.FromHtml("#3ED68C");
            Error = ColorTranslator.FromHtml("#F87171");
            Warning = ColorTranslator.FromHtml("#FBBF24");
            SuccessSoft = ColorTranslator.FromHtml("#1C3327");
            ErrorSoft = ColorTranslator.FromHtml("#3A2226");

            NavBackground = ColorTranslator.FromHtml("#191C25");
            NavCategoryText = ColorTranslator.FromHtml("#C8CCD6");
            NavLinkText = ColorTranslator.FromHtml("#7FA8FF");
            NavLinkHover = ColorTranslator.FromHtml("#A9C4FF");
            NavSelectedText = Color.White;
            NavSelectedBackground = ColorTranslator.FromHtml("#28324A");
        }

        // A distinct navy-accented dark palette (matching the idea of Visual Studio's own
        // Light/Dark/Blue theme picker) rather than just a re-tinted copy of ApplyDark - the
        // background reads as blue rather than neutral grey, and the accent leans cyan instead of
        // the app's usual indigo, so the two dark-family modes are visually distinguishable at a
        // glance rather than nearly identical.
        private static void ApplyBlue()
        {
            Background = ColorTranslator.FromHtml("#0D1B2E");
            Card = ColorTranslator.FromHtml("#152A45");
            Border = ColorTranslator.FromHtml("#274566");
            Text = ColorTranslator.FromHtml("#E5EEFA");
            TextMuted = ColorTranslator.FromHtml("#8FA8C7");
            Accent = ColorTranslator.FromHtml("#3FA9F5");
            AccentDark = ColorTranslator.FromHtml("#6FC3FF");
            AccentSoft = ColorTranslator.FromHtml("#1C3A5C");
            Success = ColorTranslator.FromHtml("#3ED68C");
            Error = ColorTranslator.FromHtml("#F87171");
            Warning = ColorTranslator.FromHtml("#FBBF24");
            SuccessSoft = ColorTranslator.FromHtml("#173B2E");
            ErrorSoft = ColorTranslator.FromHtml("#402530");

            NavBackground = ColorTranslator.FromHtml("#0A1526");
            NavCategoryText = ColorTranslator.FromHtml("#BFD6EF");
            NavLinkText = ColorTranslator.FromHtml("#6FC3FF");
            NavLinkHover = ColorTranslator.FromHtml("#A9DCFF");
            NavSelectedText = Color.White;
            NavSelectedBackground = ColorTranslator.FromHtml("#1F4468");
        }

        public static void StylePrimaryButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Accent;
            b.ForeColor = Color.White;
            b.Font = BoldFont;
            b.Cursor = Cursors.Hand;
            b.MouseEnter += (_, _) => { if (b.Enabled) b.BackColor = AccentDark; };
            b.MouseLeave += (_, _) => { if (b.Enabled) b.BackColor = Accent; };
        }

        public static void StyleSecondaryButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Border;
            b.BackColor = Card;
            b.ForeColor = Text;
            b.Font = BoldFont;
            b.Cursor = Cursors.Hand;
            b.MouseEnter += (_, _) => { if (b.Enabled) b.BackColor = AccentSoft; };
            b.MouseLeave += (_, _) => { if (b.Enabled) b.BackColor = Card; };
        }
    }
}
