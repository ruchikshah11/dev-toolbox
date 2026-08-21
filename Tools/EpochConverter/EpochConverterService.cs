using System.Globalization;

namespace DevToolbox.Tools.EpochConverter
{
    public static class EpochConverterService
    {
        // Timestamps at/above this magnitude are almost certainly milliseconds - a seconds-based
        // Unix timestamp of that size corresponds to the year 2286.
        private const long MillisecondThreshold = 10_000_000_000L;

        public static string EpochToDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new FormatException("Enter a Unix epoch timestamp (seconds or milliseconds) to convert.");
            }

            if (!long.TryParse(input.Trim(), NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var epoch))
            {
                throw new FormatException("That doesn't look like a valid integer timestamp.");
            }

            DateTimeOffset dto;
            string unit;
            try
            {
                if (Math.Abs(epoch) >= MillisecondThreshold)
                {
                    dto = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    unit = "milliseconds";
                }
                else
                {
                    dto = DateTimeOffset.FromUnixTimeSeconds(epoch);
                    unit = "seconds";
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new FormatException($"That timestamp is out of the representable date range: {ex.Message}", ex);
            }

            var utc = dto.UtcDateTime;
            var local = dto.LocalDateTime;

            return
                $"Interpreted as Unix {unit}\r\n\r\n" +
                $"UTC:   {utc:yyyy-MM-dd HH:mm:ss} UTC  ({utc:dddd, MMMM d, yyyy})\r\n" +
                $"Local: {local:yyyy-MM-dd HH:mm:ss} {TimeZoneInfo.Local.StandardName}  ({local:dddd, MMMM d, yyyy})";
        }

        public static string DateToEpoch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new FormatException("Enter a date/time to convert.");
            }

            // AssumeLocal: if the text includes an explicit offset or "Z" it's honored as-is;
            // otherwise the value is treated as local time, matching what most people expect
            // when they type a plain date/time with no timezone.
            if (!DateTimeOffset.TryParse(
                    input.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var dto))
            {
                throw new FormatException("Could not parse that as a date/time. Try a format like 2026-07-20 14:30:00.");
            }

            var seconds = dto.ToUnixTimeSeconds();
            var millis = dto.ToUnixTimeMilliseconds();

            return
                $"Parsed as: {dto:yyyy-MM-dd HH:mm:ss zzz}\r\n\r\n" +
                $"Unix seconds:      {seconds}\r\n" +
                $"Unix milliseconds: {millis}";
        }
    }
}
