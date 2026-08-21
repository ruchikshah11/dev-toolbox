using System.Windows.Forms;

namespace DevToolbox.Core
{
    /// <summary>
    /// Contract every tool (formatter, encoder, decoder, converter, ...) implements so it can
    /// be plugged into the shell's navigation and content area without the shell knowing
    /// anything about its internals.
    /// </summary>
    public interface ITool
    {
        // Groups tools in the left navigation, mirroring codebeautify.org's
        // "Formatters / JSON Formatter" style breadcrumb.
        string Category { get; }

        string Name { get; }

        string Description { get; }

        // Called once per selection so each visit gets a clean control instance.
        Control CreateView();
    }
}
