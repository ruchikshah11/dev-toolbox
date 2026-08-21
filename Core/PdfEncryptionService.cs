using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DevToolbox.Core
{
    /// <summary>
    /// Shared PdfSharp 6.x password/encryption helpers - used by both the PDF Password Remover
    /// (opens an encrypted PDF and strips its security before re-saving) and the Word to PDF
    /// converter's optional "protect with password" step (the exact same PdfSharp security APIs
    /// used in the opposite direction: applying encryption to a freshly-rendered, previously
    /// unencrypted document just before it's saved). Kept in one place so the two tools can't
    /// drift on how they talk to PdfSharp's security handler.
    /// </summary>
    public static class PdfEncryptionService
    {
        /// <summary>
        /// Opens the encrypted PDF at <paramref name="sourcePath"/> with the given password and
        /// returns it with encryption fully removed, ready to be saved unencrypted via
        /// <see cref="PdfDocument.Save(string)"/>.
        /// </summary>
        public static PdfDocument OpenAndRemoveEncryption(string sourcePath, string password)
        {
            var document = OpenWithPassword(sourcePath, password);

            // Rewrites (or, for the Import+copy fallback, sets for the first time) the security
            // handler to "no encryption", so the document opens with no password prompt at all
            // once saved.
            document.SecurityHandler.SetEncryptionToNoneAndResetPasswords();
            return document;
        }

        /// <summary>
        /// Opens the PDF, trying full Modify access first and falling back to an Import-mode
        /// page copy when only the user (not owner) password is known.
        /// </summary>
        private static PdfDocument OpenWithPassword(string sourcePath, string password)
        {
            try
            {
                // Modify mode lets the security handler be rewritten in place afterwards, but
                // PdfSharp only grants Modify access when the supplied password is (or matches)
                // the document's OWNER password.
                return PdfReader.Open(sourcePath, password, PdfDocumentOpenMode.Modify);
            }
            catch (PdfReaderException ex) when (ex.Message.Contains("owner password"))
            {
                // Only the user password is known: Modify is refused outright, but Import mode
                // accepts a correct user password. Rebuilding a fresh document by copying every
                // page across is PdfSharp's own documented workaround for producing an
                // unencrypted copy in this case.
                using var imported = PdfReader.Open(sourcePath, password, PdfDocumentOpenMode.Import);
                var rebuilt = new PdfDocument();
                foreach (var page in imported.Pages)
                {
                    rebuilt.AddPage(page);
                }
                return rebuilt;
            }
            catch (PdfReaderException ex) when (ex.Message.Contains("password is invalid"))
            {
                throw new FormatException("That password doesn't match this PDF - double-check it and try again.", ex);
            }
        }

        /// <summary>
        /// Applies password protection to <paramref name="document"/> in place - the same
        /// password is used for both the user and owner password, so opening OR fully
        /// editing/printing the saved PDF both require it. Call this after building the
        /// document's pages/content and just before <see cref="PdfDocument.Save(string)"/>.
        /// </summary>
        public static void ApplyPassword(PdfDocument document, string password)
        {
            document.SecuritySettings.UserPassword = password;
            document.SecuritySettings.OwnerPassword = password;
        }
    }
}
