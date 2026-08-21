using System.Globalization;

namespace DevToolbox.Tools.I18nStandards
{
    /// <summary>
    /// Builds the reference rows for the "I18N Standards" tool straight from the .NET runtime's
    /// own culture list rather than a hand-typed table, so the data is always accurate and
    /// complete for whatever runtime this app is running under.
    /// </summary>
    public static class I18nStandardsData
    {
        public static IEnumerable<string[]> GetRows()
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var ci in cultures)
            {
                string regionName;
                try
                {
                    regionName = new RegionInfo(ci.Name).EnglishName;
                }
                catch (ArgumentException)
                {
                    // Some specific cultures (e.g. constructed/custom or non-region-mapped
                    // cultures) have no corresponding RegionInfo and throw here.
                    regionName = string.Empty;
                }

                yield return new[] { ci.Name, ci.EnglishName, ci.TwoLetterISOLanguageName, regionName };
            }
        }
    }
}
