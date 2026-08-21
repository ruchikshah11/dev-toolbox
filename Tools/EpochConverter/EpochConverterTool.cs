using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.EpochConverter
{
    public class EpochConverterTool : ITool
    {
        public string Category => "Converters";
        public string Name => "Epoch Timestamp To Date";
        public string Description => "Converts Unix epoch timestamps to human-readable dates, and back.";

        public Control CreateView() => new TextTransformControl(
            "Enter a Unix epoch timestamp, or (for Date -> Epoch) a date/time",
            "Result",
            new[]
            {
                new TextTransformAction("Epoch -> Date", EpochConverterService.EpochToDate, Primary: true),
                new TextTransformAction("Date -> Epoch", EpochConverterService.DateToEpoch)
            },
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
    }
}
