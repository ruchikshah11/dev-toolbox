using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace DevToolbox.Tools.XmlValidator
{
    /// <summary>
    /// XML well-formedness validation, plus optional XSD schema validation, kept separate from
    /// the UI so it can be unit tested without touching WinForms. Throws FormatException (not
    /// XmlException/XmlSchemaException) on failure so the UI only has one exception type to
    /// catch.
    /// </summary>
    public static class XmlValidatorService
    {
        public static string Validate(string xml, string? xsd)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new FormatException("Nothing to validate - paste an XML document first.");
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);
            }
            catch (XmlException ex)
            {
                throw new FormatException($"Invalid XML at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
            }

            if (string.IsNullOrWhiteSpace(xsd))
            {
                return "Valid XML - the document is well-formed.";
            }

            XmlSchema schema;
            try
            {
                schema = XmlSchema.Read(new StringReader(xsd), (_, e) => throw new XmlSchemaException(e.Message));
            }
            catch (Exception ex) when (ex is XmlSchemaException or XmlException)
            {
                throw new FormatException($"Invalid XSD schema: {ex.Message}", ex);
            }

            var schemaSet = new XmlSchemaSet();
            schemaSet.Add(schema);
            try
            {
                schemaSet.Compile();
            }
            catch (XmlSchemaException ex)
            {
                throw new FormatException($"Invalid XSD schema: {ex.Message}", ex);
            }

            var errors = new List<string>();
            doc.Validate(schemaSet, (_, e) =>
            {
                var line = e.Exception?.LineNumber ?? 0;
                var pos = e.Exception?.LinePosition ?? 0;
                errors.Add(line > 0 ? $"Line {line}, position {pos}: {e.Message}" : e.Message);
            });

            return errors.Count == 0
                ? "Valid XML - well-formed and conforms to the given XSD schema."
                : $"{errors.Count} schema validation issue(s) found:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
        }
    }
}
