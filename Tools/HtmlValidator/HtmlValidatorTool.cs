using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.HtmlValidator
{
    public class HtmlValidatorTool : ITool
    {
        public string Category => "Validators";
        public string Name => "HTML Validator";
        public string Description => "Validates HTML markup against the W3C spec.";

        public Control CreateView() => new TextTransformControl(
            "Paste the HTML document to validate",
            "Validation Result",
            new[]
            {
                new TextTransformAction("Validate", HtmlValidatorService.Validate, Primary: true)
            },
            contentKind: TextTransformContentKind.Markup,
            // HtmlValidatorService never throws - it returns either a clean pass or a list of
            // issues in the same string, so success has to be read back out of the result text.
            isSuccessResult: text => text.StartsWith("No structural issues found.", StringComparison.Ordinal));
    }
}
