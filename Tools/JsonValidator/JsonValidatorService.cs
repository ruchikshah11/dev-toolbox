using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevToolbox.Tools.JsonValidator
{
    /// <summary>
    /// Pure JSON validation, kept separate from the UI so it can be unit tested without
    /// touching WinForms. Throws FormatException (not JsonReaderException) on failure so it
    /// can be plugged straight into TextTransformControl's error handling.
    ///
    /// Accepts JSONC too: Newtonsoft's JsonTextReader (what JToken.Parse uses under the hood)
    /// tolerates "//" and "/* */" comments and trailing commas by default - no extra settings
    /// needed. Comments aren't part of the JSON spec, so they don't survive into the
    /// re-serialized preview below; that's expected, not a bug.
    /// </summary>
    public static class JsonValidatorService
    {
        public static string Validate(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Nothing to validate - paste a JSON or JSONC document first.");
            }

            try
            {
                var token = JToken.Parse(json);
                var hasComments = json.Contains("//") || json.Contains("/*");
                var note = hasComments
                    ? "Valid JSON (JSONC) - comments were recognized and ignored during validation."
                    : "Valid JSON - the document parses successfully.";

                return $"{note}{Environment.NewLine}{Environment.NewLine}Re-serialized (comments removed):{Environment.NewLine}{token.ToString(Formatting.Indented)}";
            }
            catch (JsonReaderException ex)
            {
                throw new FormatException(
                    $"Invalid JSON at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}", ex);
            }
        }
    }
}
