using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace DevToolbox.Tools.YamlConverter
{
    public static class YamlJsonConversionService
    {
        public static string YamlToJson(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml))
            {
                throw new FormatException("Nothing to convert - paste some YAML first.");
            }

            object? parsed;
            try
            {
                var deserializer = new Deserializer();
                parsed = deserializer.Deserialize<object>(yaml);
            }
            catch (YamlException ex)
            {
                throw new FormatException($"Invalid YAML: {ex.Message}", ex);
            }

            var converted = ConvertYamlNode(parsed);
            return JsonConvert.SerializeObject(converted, Formatting.Indented);
        }

        public static string JsonToYaml(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Nothing to convert - paste some JSON first.");
            }

            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonReaderException ex)
            {
                throw new FormatException($"Invalid JSON: {ex.Message}", ex);
            }

            var converted = ConvertJToken(token);
            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(converted ?? new Dictionary<string, object?>());
        }

        // YamlDotNet's untyped Deserialize<object>() returns Dictionary<object,object> for
        // mappings and List<object> for sequences; Newtonsoft can't serialize non-string keys,
        // so mapping keys are stringified on the way to a JSON-friendly object graph.
        private static object? ConvertYamlNode(object? node)
        {
            switch (node)
            {
                case null:
                    return null;

                case Dictionary<object, object> map:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var kv in map)
                    {
                        var key = kv.Key?.ToString() ?? string.Empty;
                        dict[key] = ConvertYamlNode(kv.Value);
                    }
                    return dict;
                }

                case List<object> list:
                    return list.Select(ConvertYamlNode).ToList();

                default:
                    return node;
            }
        }

        // JToken -> plain CLR objects (Dictionary/List/primitives) so YamlDotNet's serializer -
        // which knows nothing about JObject/JArray/JValue - can walk the tree.
        private static object? ConvertJToken(JToken? token)
        {
            if (token is null) return null;

            switch (token.Type)
            {
                case JTokenType.Object:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        dict[prop.Name] = ConvertJToken(prop.Value);
                    }
                    return dict;
                }

                case JTokenType.Array:
                    return ((JArray)token).Select(ConvertJToken).ToList();

                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                default:
                    return ((JValue)token).Value;
            }
        }
    }
}
