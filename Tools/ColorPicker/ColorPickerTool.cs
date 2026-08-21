using DevToolbox.Core;

namespace DevToolbox.Tools.ColorPicker
{
    public class ColorPickerTool : ITool
    {
        public string Category => "Converters";
        public string Name => "Color Picker";
        public string Description => "Pick a color visually from a saturation/value gradient and hue slider, with a live Hex/RGB readout.";

        /// <summary>Creates a fresh Color Picker view instance for the shell's content area.</summary>
        public Control CreateView() => new ColorPickerControl();
    }
}
