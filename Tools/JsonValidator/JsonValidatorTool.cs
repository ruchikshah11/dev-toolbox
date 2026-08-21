using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.JsonValidator
{
    public class JsonValidatorTool : ITool
    {
        public string Category => "Validators";
        public string Name => "JSON Validator";
        public string Description => "Validates a JSON or JSONC (JSON with // and /* */ comments) document and reports syntax errors.";

        public Control CreateView() => new TextTransformControl(
            "Paste the JSON or JSONC document to validate (// and /* */ comments are allowed)",
            "Validation Result",
            new[]
            {
                new TextTransformAction("Validate", JsonValidatorService.Validate, Primary: true)
            },
            contentKind: TextTransformContentKind.Json,
            // JsonValidatorService throws on invalid input (caught and shown in the error label
            // instead), so any result that reaches the output pane is already a success.
            isSuccessResult: _ => true);
    }
}
