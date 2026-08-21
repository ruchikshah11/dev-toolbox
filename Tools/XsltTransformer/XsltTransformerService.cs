using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace DevToolbox.Tools.XsltTransformer
{
    public static class XsltTransformerService
    {
        public static string Transform(string xml, string xslt)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new FormatException("Nothing to transform - paste an XML document first.");
            }

            if (string.IsNullOrWhiteSpace(xslt))
            {
                throw new FormatException("Nothing to transform with - paste an XSLT stylesheet first.");
            }

            var xslCompiledTransform = new XslCompiledTransform();

            try
            {
                using var xsltReader = XmlReader.Create(new StringReader(xslt));
                xslCompiledTransform.Load(xsltReader);
            }
            catch (XmlException ex)
            {
                throw new FormatException($"Invalid XSLT stylesheet: {ex.Message}", ex);
            }
            catch (XsltException ex)
            {
                throw new FormatException($"Invalid XSLT stylesheet: {ex.Message}", ex);
            }

            var sb = new StringBuilder();
            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            try
            {
                using var xmlReader = XmlReader.Create(new StringReader(xml));
                using var stringWriter = new StringWriter(sb);
                using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
                {
                    xslCompiledTransform.Transform(xmlReader, null, xmlWriter);
                }
            }
            catch (XmlException ex)
            {
                throw new FormatException($"Invalid XML input: {ex.Message}", ex);
            }
            catch (XsltException ex)
            {
                throw new FormatException($"Transform failed: {ex.Message}", ex);
            }

            return sb.ToString().Trim();
        }
    }
}
