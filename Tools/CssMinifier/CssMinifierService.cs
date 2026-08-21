using NUglify;
using NUglify.Css;

namespace DevToolbox.Tools.CssMinifier
{
    // Pure NUglify wrapper shared by the CSS Beautifier and CSS Minifier tools - same source,
    // two very different CssSettings profiles.
    public static class CssMinifierService
    {
        public static string Beautify(string input)
        {
            var settings = new CssSettings
            {
                OutputMode = OutputMode.MultipleLines,
                CommentMode = CssComment.All,
                MinifyExpressions = false,
                ColorNames = CssColor.NoSwap,
                RemoveEmptyBlocks = false,
                AbbreviateHexColor = false
            };

            return Run(input, settings);
        }

        public static string Minify(string input)
        {
            // Default CssSettings already gives NUglify's standard aggressive minification:
            // OutputMode = SingleLine, hex color abbreviation, empty-block removal, etc.
            return Run(input, new CssSettings());
        }

        private static string Run(string input, CssSettings settings)
        {
            var result = Uglify.Css(input ?? string.Empty, settings, null);

            if (result.HasErrors)
            {
                var messages = string.Join("; ", result.Errors.Select(e => e.ToString()));
                throw new InvalidOperationException($"CSS could not be processed: {messages}");
            }

            return result.Code ?? string.Empty;
        }
    }
}
