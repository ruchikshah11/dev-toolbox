using System.Security.Cryptography;
using System.Text;
using DevToolbox.Core;
using Newtonsoft.Json;

namespace DevToolbox.Tools.PasswordGenerator
{
    public sealed record PasswordHistoryEntry(string Value, string Type, DateTime CreatedUtc);

    /// <summary>
    /// DPAPI-encrypted, per-user history of generated passwords/passphrases, saved under
    /// %LocalAppData%\DevToolbox in its own file, separate from settings.json - unlike app
    /// preferences (theme, pins), this file contains actual secrets, so it's encrypted at rest
    /// with Windows' own ProtectedData/DPAPI API (tied to the current Windows login) rather than
    /// stored plain.
    /// </summary>
    internal static class PasswordHistoryStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevToolbox", "password-history.dat");

        /// <summary>Loads the saved history, newest first, or an empty list if there's no file yet, or it can't be read/decrypted (e.g. copied from a different Windows account).</summary>
        public static List<PasswordHistoryEntry> Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new List<PasswordHistoryEntry>();

                var encrypted = File.ReadAllBytes(FilePath);
                var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(plain);
                return JsonConvert.DeserializeObject<List<PasswordHistoryEntry>>(json) ?? new List<PasswordHistoryEntry>();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
            {
                return new List<PasswordHistoryEntry>();
            }
        }

        /// <summary>Prepends a newly generated value to the history (capped at AppSettings.PasswordHistoryLimit) and saves immediately - a no-op if that limit is 0 (history disabled).</summary>
        public static void Add(string value, string type)
        {
            var limit = AppSettings.Load().PasswordHistoryLimit;
            if (limit <= 0) return;

            var entries = Load();
            entries.Insert(0, new PasswordHistoryEntry(value, type, DateTime.UtcNow));
            if (entries.Count > limit) entries = entries.GetRange(0, limit);
            Save(entries);
        }

        /// <summary>Deletes all saved history.</summary>
        public static void Clear() => Save(new List<PasswordHistoryEntry>());

        /// <summary>Best-effort encrypted write to disk - silently ignored if the folder isn't writable, since losing a history save isn't worth crashing over.</summary>
        private static void Save(List<PasswordHistoryEntry> entries)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var json = JsonConvert.SerializeObject(entries);
                var plain = Encoding.UTF8.GetBytes(json);
                var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(FilePath, encrypted);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
            {
                // Best-effort only - worst case, history just doesn't persist across restarts.
            }
        }
    }
}
