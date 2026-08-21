using System.Runtime.InteropServices;

namespace DevToolbox.Core
{
    /// <summary>
    /// Schedules the clipboard to be cleared some seconds after a secret (a generated password or
    /// passphrase) is copied to it, per <see cref="AppSettings.AutoClearClipboardSeconds"/> - so a
    /// copied secret doesn't sit there indefinitely. Only clears if the clipboard still holds the
    /// exact value that was copied, so it doesn't wipe out something else the user copied since.
    /// </summary>
    internal static class ClipboardAutoClear
    {
        private static readonly System.Windows.Forms.Timer PendingClear = new();
        private static string? _pendingValue;

        static ClipboardAutoClear()
        {
            PendingClear.Tick += (_, _) =>
            {
                PendingClear.Stop();
                try
                {
                    if (_pendingValue is not null && Clipboard.ContainsText() && Clipboard.GetText() == _pendingValue)
                    {
                        Clipboard.Clear();
                    }
                }
                catch (ExternalException)
                {
                    // The clipboard can be transiently locked by another process - not worth surfacing.
                }
                _pendingValue = null;
            };
        }

        /// <summary>Arms (or re-arms, replacing any pending one) the auto-clear timer for the given just-copied value, unless the setting is off.</summary>
        public static void ScheduleClear(string copiedValue)
        {
            var seconds = AppSettings.Load().AutoClearClipboardSeconds;
            if (seconds <= 0) return;

            PendingClear.Stop();
            _pendingValue = copiedValue;
            PendingClear.Interval = seconds * 1000;
            PendingClear.Start();
        }
    }
}
