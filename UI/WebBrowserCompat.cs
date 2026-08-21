using Microsoft.Win32;

namespace DevToolbox.UI
{
    /// <summary>
    /// The WebBrowser control defaults to IE7 "quirks" rendering unless the hosting exe opts into
    /// a modern IE mode via this per-user registry value, keyed to the exe's own file name -
    /// without it, any WebBrowser-hosted content renders modern HTML/CSS badly broken. The
    /// standard fix for every WinForms app using WebBrowser; shared here so every WebBrowser
    /// instance in the app (HTML Viewer's preview, the syntax-highlighted formatter outputs, ...)
    /// only needs to call this once rather than each carrying its own copy of the registry write.
    /// </summary>
    internal static class WebBrowserCompat
    {
        private static bool _done;

        public static void EnsureModernRenderingMode()
        {
            if (_done) return;
            _done = true;

            try
            {
                const string keyPath = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
                var exeName = Path.GetFileName(Application.ExecutablePath);

                using var key = Registry.CurrentUser.CreateSubKey(keyPath);
                var currentValue = key?.GetValue(exeName) as int?;
                if (currentValue is null || currentValue < 11000)
                {
                    key?.SetValue(exeName, 11001, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                // Best-effort only - if the registry write is blocked (locked-down environment),
                // WebBrowser content still works, just in legacy IE7 quirks mode.
            }
        }
    }
}
