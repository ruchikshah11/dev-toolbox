using DevToolbox.Core;

namespace DevToolbox.Tools.ClaimsIdentity
{
    public class ClaimsIdentityTool : ITool
    {
        public string Category => "SharePoint";
        public string Name => "Claims Identity Encoder / Decoder";
        public string Description => "Decodes a SharePoint claims-encoded identity string into its parts, or builds one from a claim type and value.";

        public Control CreateView() => new ClaimsIdentityControl();
    }
}
