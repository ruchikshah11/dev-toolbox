using System.Globalization;

namespace DevToolbox.Tools.CronGenerator
{
    /// <summary>
    /// Pure parsing/matching logic for the common Quartz 6-field cron subset (seconds minutes
    /// hours day-of-month month day-of-week), kept separate from the UI so it can be unit
    /// tested without touching WinForms. Supports *, ?, single values, ranges (a-b), steps
    /// (*/n or a-b/n) and comma lists. Quartz extensions L, W and # are NOT supported - ? is
    /// simply treated the same as * for computation purposes. Day-of-month and day-of-week are
    /// ANDed together (both must match), matching how the value sets are intersected here -
    /// unlike some cron dialects, this does not special-case OR semantics when both fields are
    /// restricted.
    /// </summary>
    public static class CronService
    {
        private const int MaxIterations = 500_000;
        private static readonly TimeSpan Horizon = TimeSpan.FromDays(365 * 2);

        public sealed class ParsedCron
        {
            public HashSet<int> Seconds { get; set; } = new();
            public HashSet<int> Minutes { get; set; } = new();
            public HashSet<int> Hours { get; set; } = new();
            public HashSet<int> DaysOfMonth { get; set; } = new();
            public HashSet<int> Months { get; set; } = new();
            public HashSet<int> DaysOfWeek { get; set; } = new();
        }

        public static ParsedCron Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new FormatException("Enter a 6-field Quartz cron expression, e.g. \"0 0/5 * * * ?\".");
            }

            var fields = expression.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 6)
            {
                throw new FormatException(
                    $"Expected 6 fields (seconds minutes hours day-of-month month day-of-week), found {fields.Length}.");
            }

            return new ParsedCron
            {
                Seconds = ParseField(fields[0], 0, 59, "seconds"),
                Minutes = ParseField(fields[1], 0, 59, "minutes"),
                Hours = ParseField(fields[2], 0, 23, "hours"),
                DaysOfMonth = ParseField(fields[3], 1, 31, "day-of-month"),
                Months = ParseField(fields[4], 1, 12, "month"),
                DaysOfWeek = ParseField(fields[5], 0, 6, "day-of-week"),
            };
        }

        public static List<DateTime> GetNextFireTimes(ParsedCron cron, DateTime start, int count)
        {
            var results = new List<DateTime>();
            // Truncate to the second and begin one second later, so an already-elapsed instant
            // within the current second is never returned as a "next" fire time.
            var current = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second)
                .AddSeconds(1);
            var horizonEnd = start.Add(Horizon);

            var iterations = 0;
            while (current <= horizonEnd && iterations < MaxIterations && results.Count < count)
            {
                iterations++;
                if (Matches(cron, current))
                {
                    results.Add(current);
                }
                current = current.AddSeconds(1);
            }

            if (results.Count == 0)
            {
                throw new FormatException(
                    "No matching fire time was found within the search horizon (2 years, capped at " +
                    $"{MaxIterations:N0} seconds scanned) - check the expression.");
            }

            return results;
        }

        private static bool Matches(ParsedCron cron, DateTime dt) =>
            cron.Seconds.Contains(dt.Second)
            && cron.Minutes.Contains(dt.Minute)
            && cron.Hours.Contains(dt.Hour)
            && cron.DaysOfMonth.Contains(dt.Day)
            && cron.Months.Contains(dt.Month)
            && cron.DaysOfWeek.Contains((int)dt.DayOfWeek);

        private static HashSet<int> ParseField(string field, int min, int max, string fieldName)
        {
            var result = new HashSet<int>();

            if (field == "*" || field == "?")
            {
                for (var v = min; v <= max; v++) result.Add(v);
                return result;
            }

            foreach (var token in field.Split(','))
            {
                ParseToken(token, min, max, fieldName, result);
            }

            if (result.Count == 0)
            {
                throw new FormatException($"The {fieldName} field \"{field}\" did not resolve to any values.");
            }

            return result;
        }

        private static void ParseToken(string token, int min, int max, string fieldName, HashSet<int> result)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new FormatException($"Empty value in the {fieldName} field.");
            }

            var rangePart = token;
            var step = 1;

            var slashIndex = token.IndexOf('/');
            if (slashIndex >= 0)
            {
                rangePart = token.Substring(0, slashIndex);
                var stepPart = token.Substring(slashIndex + 1);
                if (!int.TryParse(stepPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out step) || step <= 0)
                {
                    throw new FormatException($"Invalid step \"{stepPart}\" in the {fieldName} field.");
                }
            }

            int rangeStart, rangeEnd;
            if (rangePart == "*" || rangePart == "?")
            {
                rangeStart = min;
                rangeEnd = max;
            }
            else if (rangePart.Contains('-'))
            {
                var parts = rangePart.Split('-');
                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeStart)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeEnd))
                {
                    throw new FormatException($"Invalid range \"{rangePart}\" in the {fieldName} field.");
                }
            }
            else
            {
                if (!int.TryParse(rangePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeStart))
                {
                    throw new FormatException($"Invalid value \"{rangePart}\" in the {fieldName} field.");
                }
                rangeEnd = rangeStart;
            }

            if (rangeStart < min || rangeEnd > max || rangeStart > rangeEnd)
            {
                throw new FormatException(
                    $"Value \"{rangePart}\" is out of range for the {fieldName} field ({min}-{max}).");
            }

            for (var v = rangeStart; v <= rangeEnd; v += step)
            {
                result.Add(v);
            }
        }
    }
}
