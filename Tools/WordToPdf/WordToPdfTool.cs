using DevToolbox.Core;

namespace DevToolbox.Tools.WordToPdf
{
    /// <summary>ITool registration for the Word to PDF converter.</summary>
    public class WordToPdfTool : ITool
    {
        public string Category => "PDF Tools";
        public string Name => "Word to PDF";
        public string Description => "Converts a .docx to PDF - headings, paragraph text, and bold/italic emphasis are preserved; tables, images, and multi-column layouts are not. Optionally protect the output with a password.";

        /// <summary>Creates the Word to PDF's file-picker + convert + save control.</summary>
        public Control CreateView() => new WordToPdfControl();
    }
}
