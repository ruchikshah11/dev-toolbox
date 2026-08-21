using DevToolbox.Core;

namespace DevToolbox.Tools.RotatePdf
{
    /// <summary>ITool registration for Rotate PDF Pages.</summary>
    public class RotatePdfTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Rotate PDF Pages";
        public string Description => "Rotates all (or just the pages you specify) of a PDF's pages by 90, 180, or 270 degrees and saves the result.";

        /// <summary>Creates the Rotate PDF Pages' file-picker + rotation control.</summary>
        public Control CreateView() => new RotatePdfControl();
    }
}
