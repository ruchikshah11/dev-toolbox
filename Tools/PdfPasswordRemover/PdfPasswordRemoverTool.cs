using DevToolbox.Core;

namespace DevToolbox.Tools.PdfPasswordRemover
{
    /// <summary>ITool registration for the PDF Password Remover.</summary>
    public class PdfPasswordRemoverTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "PDF Password Remover";
        public string Description => "Removes password protection from an encrypted PDF (RC4, AES-128, or AES-256) and saves an unlocked copy.";

        /// <summary>Creates the PDF Password Remover's file-picker + password + save control.</summary>
        public Control CreateView() => new PdfPasswordRemoverControl();
    }
}
