namespace DevToolbox.Tools.TimezoneConverter
{
    public readonly record struct TimezoneConversionResult(string ZoneId, string DisplayName, DateTime LocalTime, TimeSpan UtcOffset);

    public static class TimezoneConverterService
    {
        // Every zone Windows knows about (~140), sorted the same way Windows' own timezone
        // picker sorts them - by UTC offset, then name - the natural order for scanning
        // "what's the time somewhere west/east of me".
        public static IReadOnlyList<TimeZoneInfo> AllZones { get; } =
            TimeZoneInfo.GetSystemTimeZones().OrderBy(z => z.BaseUtcOffset).ThenBy(z => z.DisplayName).ToList();

        /// <summary>Converts one date/time from the given source zone into every zone Windows knows about.</summary>
        public static List<TimezoneConversionResult> ConvertToAllZones(DateTime input, TimeZoneInfo sourceZone)
        {
            var utc = ConvertToUtc(input, sourceZone);

            return AllZones
                .Select(zone => new TimezoneConversionResult(zone.Id, zone.DisplayName, TimeZoneInfo.ConvertTimeFromUtc(utc, zone), zone.GetUtcOffset(utc)))
                .ToList();
        }

        /// <summary>Returns the current instant expressed as wall-clock time in the given zone (for the "Now" button).</summary>
        public static DateTime NowInZone(TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);

        /// <summary>Formats a UTC offset as "UTC+05:30" / "UTC-04:00".</summary>
        public static string FormatOffset(TimeSpan offset)
        {
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            return $"UTC{sign}{offset.Duration():hh\\:mm}";
        }

        /// <summary>Interprets a wall-clock value as belonging to the given source zone and converts it to UTC.</summary>
        private static DateTime ConvertToUtc(DateTime input, TimeZoneInfo sourceZone)
        {
            // Unspecified (rather than the DateTimePicker's default Kind, which .NET treats as
            // Local) lets ConvertTimeToUtc interpret the value against *any* chosen source zone
            // instead of throwing when the source zone isn't the machine's own local zone.
            var unspecified = DateTime.SpecifyKind(input, DateTimeKind.Unspecified);

            try
            {
                return TimeZoneInfo.ConvertTimeToUtc(unspecified, sourceZone);
            }
            catch (ArgumentException ex)
            {
                // Thrown for times that don't exist in the source zone (the hour skipped by a
                // spring-forward DST transition).
                throw new FormatException($"That date/time does not exist in {sourceZone.DisplayName} (likely a DST transition gap).", ex);
            }
        }
    }
}
