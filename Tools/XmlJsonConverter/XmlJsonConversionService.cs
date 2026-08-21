using System.Xml.Linq;
using Newtonsoft.Json;

namespace DevToolbox.Tools.XmlJsonConverter
{
    public static class XmlJsonConversionService
    {
        public static string XmlToJson(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new FormatException("Nothing to convert - paste an XML document first.");
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new FormatException($"Invalid XML: {ex.Message}", ex);
            }

            try
            {
                return JsonConvert.SerializeXNode(doc, Formatting.Indented);
            }
            catch (Exception ex) when (ex is not FormatException)
            {
                throw new FormatException($"Could not convert this XML to JSON: {ex.Message}", ex);
            }
        }

        public static string JsonToXml(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Nothing to convert - paste some JSON first.");
            }

            try
            {
                var doc = JsonConvert.DeserializeXNode(json, "Root");
                if (doc is null)
                {
                    throw new FormatException("The JSON did not convert to any XML content.");
                }

                return doc.ToString();
            }
            catch (FormatException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new FormatException(
                    $"Could not convert this JSON to XML: {ex.Message} (JSON arrays or multiple top-level properties may need a wrapping object first).", ex);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Could not convert this JSON to XML: {ex.Message}", ex);
            }
        }
    }
}
