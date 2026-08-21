using DevToolbox.Core;

namespace DevToolbox.Tools.ProtectPdf
{
    /// <summary>ITool registration for Protect PDF (Add Password).</summary>
    public class ProtectPdfTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Protect PDF (Add Password)";
        public string Description => "Adds password protection to a PDF - a user password (required to open it), an owner password (required to change permissions), or both - and saves an encrypted copy.";

        /// <summary>Creates the Protect PDF's file-picker + password + save control.</summary>
        public Control CreateView() => new ProtectPdfControl();
    }
}
