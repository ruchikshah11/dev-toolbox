using System.Text;
using System.Text.RegularExpressions;

namespace DevToolbox.Tools.SharePointInternalName
{
    /// <summary>
    /// Best-effort implementation of SharePoint's "_xHHHH_" internal name encoding, used to turn
    /// a column/list display name into a safe internal name (e.g. "Due Date" -> "Due_x0020_Date").
    /// Covers the common case (encode anything that isn't a plain ASCII letter/digit/underscore)
    /// rather than every edge case SharePoint itself handles (e.g. names that would otherwise
    /// collide with an existing "_xHHHH_" sequence).
    /// </summary>
    public static class SharePointInternalNameService
    {
        public static string Encode(string input)
        {
            input ??= string.Empty;
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                if (c == '_' || (char.IsLetterOrDigit(c) && c < 128)) sb.Append(c);
                else sb.Append($"_x{(int)c:x4}_");
            }
            return sb.ToString();
        }

        public static string Decode(string input)
        {
            input ??= string.Empty;
            return Regex.Replace(input, "_x([0-9a-fA-F]{4})_",
                m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        }
    }
}
