using DevToolbox.Core;
using PdfSharp.Pdf.IO;

namespace DevToolbox.Tools.ProtectPdf
{
    /// <summary>
    /// Adds password protection to an unencrypted PDF. Reuses
    /// <see cref="PdfEncryptionService.ApplyPassword"/> - the same helper the Word to PDF
    /// converter's optional "protect with password" step already uses - for the common case
    /// where the same password should be required both to open the file and to change its
    /// permissions. When the caller supplies two different passwords (open vs. permissions), the
    /// two <see cref="PdfSharp.Pdf.Security.PdfStandardSecurityHandler"/> properties are set
    /// directly instead, since <see cref="PdfEncryptionService.ApplyPassword"/> only supports one
    /// shared password for both roles - that direct assignment is just setting two properties,
    /// not re-implementing any of PdfSharp's actual security-handler mechanics.
    /// </summary>
    public static class ProtectPdfService
    {
        /// <summary>
        /// Opens <paramref name="sourcePath"/>, applies <paramref name="userPassword"/> (required
        /// to open) and/or <paramref name="ownerPassword"/> (required to change permissions) -
        /// at least one must be non-empty - and saves the encrypted result to
        /// <paramref name="destinationPath"/>.
        /// </summary>
        public static void Protect(string sourcePath, string destinationPath, string? userPassword, string? ownerPassword)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected PDF file could not be found.", sourcePath);
            }

            var hasUser = !string.IsNullOrEmpty(userPassword);
            var hasOwner = !string.IsNullOrEmpty(ownerPassword);
            if (!hasUser && !hasOwner)
            {
                throw new ArgumentException("Enter a user password, an owner password, or both.");
            }

            var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);

            if (hasUser && hasOwner && userPassword == ownerPassword)
            {
                PdfEncryptionService.ApplyPassword(document, userPassword!);
            }
            else
            {
                if (hasUser) document.SecuritySettings.UserPassword = userPassword!;
                if (hasOwner) document.SecuritySettings.OwnerPassword = ownerPassword!;
            }

            document.Save(destinationPath);
        }
    }
}
