using DevToolbox.Tools.AesEncryption;
using DevToolbox.Tools.Base32Encoder;
using DevToolbox.Tools.Base64Encoder;
using DevToolbox.Tools.CamlFormatter;
using DevToolbox.Tools.CidrCalculator;
using DevToolbox.Tools.ClaimsIdentity;
using DevToolbox.Tools.CodeRunner;
using DevToolbox.Tools.ColorConverter;
using DevToolbox.Tools.ColorPicker;
using DevToolbox.Tools.CompressImage;
using DevToolbox.Tools.CompressPdf;
using DevToolbox.Tools.CreditCardTool;
using DevToolbox.Tools.CronGenerator;
using DevToolbox.Tools.CssMinifier;
using DevToolbox.Tools.CsvConverter;
using DevToolbox.Tools.CsvEscape;
using DevToolbox.Tools.DiffViewer;
using DevToolbox.Tools.EpochConverter;
using DevToolbox.Tools.FileEncodingConverter;
using DevToolbox.Tools.GuidGenerator;
using DevToolbox.Tools.HmacGenerator;
using DevToolbox.Tools.HtmlEntities;
using DevToolbox.Tools.HtmlEscape;
using DevToolbox.Tools.HtmlFormatter;
using DevToolbox.Tools.HtmlValidator;
using DevToolbox.Tools.HtmlViewer;
using DevToolbox.Tools.HttpStatusCodes;
using DevToolbox.Tools.I18nStandards;
using DevToolbox.Tools.ImagePreviewer;
using DevToolbox.Tools.JavaDotNetEscape;
using DevToolbox.Tools.JavaRegexTester;
using DevToolbox.Tools.JavaScriptEscape;
using DevToolbox.Tools.JsonEscape;
using DevToolbox.Tools.JsMinifier;
using DevToolbox.Tools.JsonFormatter;
using DevToolbox.Tools.JsonValidator;
using DevToolbox.Tools.JwtDecoder;
using DevToolbox.Tools.JwtEncoder;
using DevToolbox.Tools.CertificateDecoder;
using DevToolbox.Tools.LoremIpsum;
using DevToolbox.Tools.MarkdownPreviewer;
using DevToolbox.Tools.MergePdf;
using DevToolbox.Tools.MessageDigester;
using DevToolbox.Tools.MimeTypes;
using DevToolbox.Tools.NumberBaseConverter;
using DevToolbox.Tools.PasswordGenerator;
using DevToolbox.Tools.PdfPageNumbers;
using DevToolbox.Tools.PdfPasswordRemover;
using DevToolbox.Tools.PdfToMarkdown;
using DevToolbox.Tools.PdfToWord;
using DevToolbox.Tools.PdfWatermark;
using DevToolbox.Tools.ProtectPdf;
using DevToolbox.Tools.WordToPdf;
using DevToolbox.Tools.QrCodeGenerator;
using DevToolbox.Tools.RegexTester;
using DevToolbox.Tools.RestApiReference;
using DevToolbox.Tools.RotatePdf;
using DevToolbox.Tools.SharePointInternalName;
using DevToolbox.Tools.SplitPdf;
using DevToolbox.Tools.SqlEscape;
using DevToolbox.Tools.SqlFormatter;
using DevToolbox.Tools.StringUtilities;
using DevToolbox.Tools.TimezoneConverter;
using DevToolbox.Tools.UrlEncoder;
using DevToolbox.Tools.UrlParser;
using DevToolbox.Tools.XmlEscape;
using DevToolbox.Tools.XmlFormatter;
using DevToolbox.Tools.XmlJsonConverter;
using DevToolbox.Tools.XmlValidator;
using DevToolbox.Tools.XPathTester;
using DevToolbox.Tools.XsdGenerator;
using DevToolbox.Tools.XsltTransformer;
using DevToolbox.Tools.YamlConverter;

namespace DevToolbox.Core
{
    /// <summary>
    /// Single place that lists every tool available in the app, matching the category/tool
    /// layout of freeformatter.com's sidebar. Adding a new formatter, encoder, converter etc.
    /// in the future is a one-line swap of a PlaceholderTool entry for a real ITool - the
    /// shell (MainForm) never needs to change.
    /// </summary>
    public static class ToolRegistry
    {
        public static IReadOnlyList<ITool> All { get; } = BuildAll();

        /// <summary>Constructs one instance of every tool in the app, in nav display order.</summary>
        private static List<ITool> BuildAll()
        {
            return new List<ITool>
            {
                // Formatters
                new XmlFormatterTool(),
                new JsonFormatterTool(),
                new HtmlFormatterTool(),
                new SqlFormatterTool(),

                // Validators
                new XmlValidatorTool(),
                new JsonValidatorTool(),
                new HtmlValidatorTool(),
                new XPathTesterTool(),
                new CreditCardTool(),
                new RegexTesterTool(),
                new JavaRegexTesterTool(),
                new CronGeneratorTool(),

                // Converters
                new XsdGeneratorTool(),
                new XsltTransformerTool(),
                new XmlToJsonTool(),
                new JsonToXmlTool(),
                new CsvToXmlTool(),
                new CsvToJsonTool(),
                new YamlToJsonTool(),
                new JsonToYamlTool(),
                new EpochConverterTool(),
                new TimezoneConverterTool(),
                new ColorConverterTool(),
                new ColorPickerTool(),
                new NumberBaseConverterTool(),
                new CidrCalculatorTool(),

                // PDF Tools
                new PdfPasswordRemoverTool(),
                new WordToPdfTool(),
                new PdfToWordTool(),
                new PdfToMarkdownTool(),
                new MergePdfTool(),
                new SplitPdfTool(),
                new RotatePdfTool(),
                new PdfPageNumbersTool(),
                new PdfWatermarkTool(),
                new ProtectPdfTool(),
                new CompressPdfTool(),

                // Code Runner
                new CodeRunnerTool(),

                // Encoders / Cryptography
                new UrlEncoderTool(),
                new Base64EncoderTool(),
                new Base32EncoderTool(),
                new FileEncodingConverterTool(),
                new MessageDigesterTool(),
                new HmacGeneratorTool(),
                new AesEncryptionTool(),
                new JwtDecoderTool(),
                new JwtEncoderTool(),
                new CertificateDecoderTool(),
                new ImagePreviewerTool(),
                new CompressImageTool(),
                new QrCodeGeneratorTool(),
                new GuidGeneratorTool(),
                new PasswordGeneratorTool(),

                // Code Minifiers / Beautifier
                new JsBeautifierTool(),
                new JsMinifierTool(),
                new CssBeautifierTool(),
                new CssMinifierTool(),

                // String Escaper & Utilities
                new StringUtilitiesTool(),
                new HtmlEscapeTool(),
                new XmlEscapeTool(),
                new JavaDotNetEscapeTool(),
                new JavaScriptEscapeTool(),
                new JsonEscapeTool(),
                new CsvEscapeTool(),
                new SqlEscapeTool(),
                new DiffViewerTool(),

                // Web Resources
                new LoremIpsumTool(),
                new HtmlViewerTool(),
                new MarkdownPreviewerTool(),
                new MimeTypesTool(),
                new HtmlEntitiesTool(),
                new UrlParserTool(),
                new I18nStandardsTool(),
                new HttpStatusCodesTool(),

                // SharePoint
                new SharePointInternalNameTool(),
                new ClaimsIdentityTool(),
                new CamlFormatterTool(),
                new RestApiReferenceTool(),
            };
        }
    }
}
