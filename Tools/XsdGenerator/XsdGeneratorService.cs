using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace DevToolbox.Tools.XsdGenerator
{
    public static class XsdGeneratorService
    {
        public static string GenerateXsd(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new FormatException("Nothing to infer a schema from - paste a sample XML document first.");
            }

            XmlSchemaSet schemaSet;
            try
            {
                using var stringReader = new StringReader(xml);
                using var xmlReader = XmlReader.Create(stringReader);
                var inference = new XmlSchemaInference();
                schemaSet = inference.InferSchema(xmlReader);
            }
            catch (XmlException ex)
            {
                throw new FormatException($"Invalid XML: {ex.Message}", ex);
            }
            catch (XmlSchemaInferenceException ex)
            {
                throw new FormatException($"Could not infer a schema: {ex.Message}", ex);
            }

            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            var schemas = schemaSet.Schemas().Cast<XmlSchema>().ToList();
            if (schemas.Count == 0)
            {
                throw new FormatException("No schema could be inferred from the supplied XML.");
            }

            foreach (var schema in schemas)
            {
                using var stringWriter = new StringWriter(sb);
                using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    schema.Write(xmlWriter);
                }
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
    }
}
