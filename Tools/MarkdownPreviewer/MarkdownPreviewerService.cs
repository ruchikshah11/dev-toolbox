using Markdig;

namespace DevToolbox.Tools.MarkdownPreviewer
{
    /// <summary>
    /// Wraps Markdig (a CommonMark-compliant Markdown processor) to render Markdown into a
    /// complete, minimally-styled HTML document for the live preview pane.
    /// </summary>
    public static class MarkdownPreviewerService
    {
        // Built once and reused - MarkdownPipeline is immutable and safe to share across calls.
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        private const string StyleBlock = @"
            html, body { max-width: 100%; overflow-x: hidden; }
            body { font-family: Segoe UI, Arial, sans-serif; font-size: 14px; color: #1c2233; padding: 16px 20px;
                   word-wrap: break-word; overflow-wrap: break-word; }
            h1, h2, h3, h4, h5, h6 { font-weight: 600; margin-top: 20px; margin-bottom: 10px; }
            h1 { border-bottom: 1px solid #dde1ea; padding-bottom: 6px; }
            h2 { border-bottom: 1px solid #dde1ea; padding-bottom: 4px; }
            code { font-family: Consolas, monospace; background: #f2f4f9; padding: 1px 5px; border-radius: 3px;
                   word-wrap: break-word; overflow-wrap: break-word; }
            /* Code blocks scroll horizontally on purpose - wrapping code mid-line is worse than a scrollbar. */
            pre { background: #f2f4f9; padding: 10px 14px; border-radius: 4px; overflow-x: auto; max-width: 100%; }
            pre code { background: none; padding: 0; }
            blockquote { border-left: 4px solid #dde1ea; margin: 0; padding: 2px 14px; color: #6b7280; }
            /* table-layout: fixed + a 100% width stops one long cell from stretching the whole
               document wider than the pane, which was pushing every other line off the right
               edge with no way to see it - see the note on this in HtmlViewerControl's preview. */
            table { border-collapse: collapse; width: 100%; table-layout: fixed; }
            th, td { border: 1px solid #dde1ea; padding: 6px 10px; word-wrap: break-word; overflow-wrap: break-word; }
            a { color: #2f6fed; }
            img { max-width: 100%; }";

        /// <summary>Converts Markdown source into a full HTML document (with the preview's own stylesheet embedded) ready to hand to a WebBrowser control.</summary>
        public static string ToHtmlDocument(string markdown)
        {
            var body = Markdown.ToHtml(markdown ?? string.Empty, Pipeline);
            // The doctype puts the WebBrowser control into standards mode (paired with the
            // FEATURE_BROWSER_EMULATION registry key) - without it, CSS like table-layout/
            // word-wrap above renders inconsistently under IE's quirks mode.
            return $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>{StyleBlock}</style></head><body>{body}</body></html>";
        }
    }
}
