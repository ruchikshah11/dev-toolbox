using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace DevToolbox.Tools.JsonFormatter
{
    // Builds the collapsible TreeView representation of a parsed JSON document. Each node's
    // display text is set to the full "key: value" text (so TreeView sizes/selects it
    // correctly) while Tag carries the key/value split so JsonOutputView's owner-draw
    // handler can paint the two halves in different colors.
    internal static class JsonTreeBuilder
    {
        public readonly record struct NodeTagInfo(string KeyText, string Separator, string ValueText, Color KeyColor, Color ValueColor);

        public static TreeNode BuildNode(string label, JToken token) => BuildNode(label, token, isArrayIndex: false);

        private static TreeNode BuildNode(string label, JToken token, bool isArrayIndex)
        {
            var labelColor = isArrayIndex ? JsonColors.Structural : JsonColors.Key;

            switch (token.Type)
            {
                case JTokenType.Object:
                    var obj = (JObject)token;
                    var props = obj.Properties().ToList();
                    var objSummary = props.Count == 0
                        ? "{}"
                        : $"{{ {props.Count} {(props.Count == 1 ? "field" : "fields")} }}";
                    var objNode = MakeNode(label, " ", objSummary, labelColor, JsonColors.ContainerSummary);
                    foreach (var prop in props)
                    {
                        objNode.Nodes.Add(BuildNode(prop.Name, prop.Value, isArrayIndex: false));
                    }
                    return objNode;

                case JTokenType.Array:
                    var arr = (JArray)token;
                    var arrSummary = arr.Count == 0
                        ? "[]"
                        : $"[ {arr.Count} {(arr.Count == 1 ? "item" : "items")} ]";
                    var arrNode = MakeNode(label, " ", arrSummary, labelColor, JsonColors.ContainerSummary);
                    for (var i = 0; i < arr.Count; i++)
                    {
                        arrNode.Nodes.Add(BuildNode($"[{i}]", arr[i], isArrayIndex: true));
                    }
                    return arrNode;

                default:
                    var (valueText, valueColor) = FormatScalar((JValue)token);
                    return MakeNode(label, ": ", valueText, labelColor, valueColor);
            }
        }

        private static TreeNode MakeNode(string key, string separator, string value, Color keyColor, Color valueColor)
        {
            return new TreeNode(key + separator + value)
            {
                Tag = new NodeTagInfo(key, separator, value, keyColor, valueColor)
            };
        }

        private static (string Text, Color Color) FormatScalar(JValue value) => value.Type switch
        {
            JTokenType.String => (value.ToString(Formatting.None), JsonColors.StringValue),
            JTokenType.Integer or JTokenType.Float => (value.ToString(Formatting.None), JsonColors.Number),
            JTokenType.Boolean => (value.ToString(Formatting.None), JsonColors.Boolean),
            JTokenType.Null => ("null", JsonColors.Null),
            _ => (value.ToString(Formatting.None), JsonColors.StringValue)
        };
    }
}
