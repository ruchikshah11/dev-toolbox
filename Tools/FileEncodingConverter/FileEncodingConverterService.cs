using System.Text;

namespace DevToolbox.Tools.FileEncodingConverter
{
    public static class FileEncodingConverterService
    {
        public static string ReadText(string path, Encoding sourceEncoding)
        {
            var bytes = File.ReadAllBytes(path);
            return sourceEncoding.GetString(bytes);
        }

        public static void WriteText(string path, string text, Encoding targetEncoding)
        {
            File.WriteAllText(path, text, targetEncoding);
        }
    }
}
