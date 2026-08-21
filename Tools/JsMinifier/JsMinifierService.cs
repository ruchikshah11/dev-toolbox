using NUglify;
using NUglify.JavaScript;

namespace DevToolbox.Tools.JsMinifier
{
    // Pure NUglify wrapper shared by the JavaScript Beautifier and JavaScript Minifier tools -
    // same source, two very different CodeSettings profiles.
    public static class JsMinifierService
    {
        public static string Beautify(string input)
        {
            var settings = new CodeSettings
            {
                MinifyCode = false,
                OutputMode = OutputMode.MultipleLines,
                LocalRenaming = LocalRenaming.KeepAll,
                RemoveUnneededCode = false,
                PreserveImportantComments = true,
                TermSemicolons = true
            };

            return Run(input, settings);
        }

        public static string Minify(string input)
        {
            // Default CodeSettings already gives NUglify's standard aggressive minification:
            // MinifyCode = true, OutputMode = SingleLine, LocalRenaming = CrunchAll.
            return Run(input, new CodeSettings());
        }

        private static string Run(string input, CodeSettings settings)
        {
            var result = Uglify.Js(input ?? string.Empty, settings);

            if (result.HasErrors)
            {
                var messages = string.Join("; ", result.Errors.Select(e => e.ToString()));
                throw new InvalidOperationException($"JavaScript could not be processed: {messages}");
            }

            return result.Code ?? string.Empty;
        }
    }
}
