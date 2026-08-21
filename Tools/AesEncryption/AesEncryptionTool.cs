using DevToolbox.Core;

namespace DevToolbox.Tools.AesEncryption
{
    /// <summary>ITool registration for the AES Encrypt/Decrypt tool.</summary>
    public class AesEncryptionTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "AES Encrypt / Decrypt";

        public string Description =>
            "Encrypts text with a password using AES-256-GCM, producing a single self-contained "
            + "Base64 blob - or decrypts one back to plain text with the same password. Uses "
            + "authenticated encryption, so a wrong password or corrupted/tampered ciphertext "
            + "fails with a clear error rather than silently producing garbage.";

        public Control CreateView() => new AesEncryptionControl();
    }
}
