using DevToolbox.Core;

namespace DevToolbox.Tools.NumberBaseConverter
{
    public class NumberBaseConverterTool : ITool
    {
        public string Category => "Converters";
        public string Name => "Number Base Converter";
        public string Description => "Converts a number between binary, octal, decimal, and hexadecimal.";

        /// <summary>Creates the Number Base Converter's input/output control.</summary>
        public Control CreateView() => new NumberBaseConverterControl();
    }
}
