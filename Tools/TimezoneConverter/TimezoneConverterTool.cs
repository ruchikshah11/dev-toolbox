using DevToolbox.Core;

namespace DevToolbox.Tools.TimezoneConverter
{
    public class TimezoneConverterTool : ITool
    {
        public string Category => "Converters";
        public string Name => "Timezone Converter";
        public string Description => "Converts one date/time across UTC, your local zone, and a handful of commonly-needed timezones at once.";

        /// <summary>Creates the Timezone Converter's input/output control.</summary>
        public Control CreateView() => new TimezoneConverterControl();
    }
}
