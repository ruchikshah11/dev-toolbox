using DevToolbox.Core;

namespace DevToolbox.Tools.FileEncodingConverter
{
    public class FileEncodingConverterTool : ITool
    {
        public string Category => "Encoders / Cryptography";
        public string Name => "Convert File Encoding";
        public string Description => "Reads a text file with one encoding and saves it back out with another.";

        public Control CreateView() => new FileEncodingConverterControl();
    }
}
