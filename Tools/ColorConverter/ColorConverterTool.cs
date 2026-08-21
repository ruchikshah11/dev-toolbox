using DevToolbox.Core;

namespace DevToolbox.Tools.ColorConverter
{
    public class ColorConverterTool : ITool
    {
        public string Category => "Converters";
        public string Name => "Color Converter";
        public string Description => "Converts colors between HEX, RGB and HSL, with a live swatch preview.";

        public Control CreateView() => new ColorConverterControl();
    }
}
