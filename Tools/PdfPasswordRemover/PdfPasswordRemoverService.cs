using DevToolbox.Core;

namespace DevToolbox.Tools.PdfPasswordRemover
{
    /// <summary>
    /// Strips password protection/encryption from a PDF using PdfSharp 6.x (the modernized
    /// empira line, not the old 1.50 series - 6.2+ is the first release able to read
    /// revision-5/6 (AES-256-ish) encrypted PDFs in addition to classic RC4/AES-128). The actual
    /// PdfSharp open/strip mechanics live in <see cref="PdfEncryptionService"/>, shared with the
    /// Word to PDF converter's "protect with password" step, so the two tools can't drift on how
    /// they talk to PdfSharp's security handler.
    /// </summary>
    public static class PdfPasswordRemoverService
    {
        /// <summary>
        /// Opens the encrypted PDF at <paramref name="sourcePath"/> with the given password and
        /// writes an unencrypted copy to <paramref name="destinationPath"/>.
        /// </summary>
        public static void RemovePassword(string sourcePath, string password, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            var document = PdfEncryptionService.OpenAndRemoveEncryption(sourcePath, password);
            document.Save(destinationPath);
        }
    }
}
