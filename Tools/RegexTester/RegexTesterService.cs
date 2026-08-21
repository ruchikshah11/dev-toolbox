using System.Text;
using System.Text.RegularExpressions;

namespace DevToolbox.Tools.RegexTester
{
    /// <summary>
    /// Pure regex evaluation/reporting logic, kept separate from the UI so it can be unit
    /// tested without touching WinForms.
    /// </summary>
    public static class RegexTesterService
    {
        public static string Test(string pattern, string input, RegexOptions options)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new FormatException("Enter a regular expression pattern first.");
            }

            Regex regex;
            try
            {
                regex = new Regex(pattern, options);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException($"Invalid pattern: {ex.Message}", ex);
            }

            var matches = regex.Matches(input ?? string.Empty);
            var sb = new StringBuilder();

            if (matches.Count == 0)
            {
                sb.AppendLine("Match: no.");
                return sb.ToString();
            }

            sb.AppendLine($"Match: yes - {matches.Count} match(es) found.");
            sb.AppendLine();

            var groupNames = regex.GetGroupNames();
            var index = 1;
            foreach (Match match in matches)
            {
                sb.AppendLine($"--- Match {index} ---");
                sb.AppendLine($"Index: {match.Index}");
                sb.AppendLine($"Length: {match.Length}");
                sb.AppendLine($"Value: {match.Value}");

                foreach (var groupName in groupNames)
                {
                    if (groupName == "0") continue; // group 0 is the whole match, already shown above
                    var group = match.Groups[groupName];
                    if (!group.Success) continue;
                    sb.AppendLine($"Group '{groupName}': {group.Value}");
                }

                sb.AppendLine();
                index++;
            }

            return sb.ToString();
        }
    }
}
