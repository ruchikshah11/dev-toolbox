using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.SharePointInternalName
{
    public class SharePointInternalNameTool : ITool
    {
        public string Category => "SharePoint";
        public string Name => "Internal Name Encoder / Decoder";
        public string Description => "Converts between a SharePoint column/list display name and its _xHHHH_-encoded internal name.";

        public Control CreateView() => new TextTransformControl(
            "Enter a display name or an internal name",
            "Result",
            new[]
            {
                new TextTransformAction("Encode (Display -> Internal)", SharePointInternalNameService.Encode, Primary: true),
                new TextTransformAction("Decode (Internal -> Display)", SharePointInternalNameService.Decode)
            });
    }
}
