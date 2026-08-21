using DevToolbox.Core;

namespace DevToolbox.Tools.ImagePreviewer
{
    public class ImagePreviewerTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Image / Data URI Previewer";
        public string Description => "Previews an image from a pasted data URI or bare base64 string, or converts an image file into its base64/data URI form.";

        /// <summary>Creates the Image Previewer's paste/upload + preview control.</summary>
        public Control CreateView() => new ImagePreviewerControl();
    }
}
