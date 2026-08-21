using System.Windows.Forms;
using DevToolbox.UI;

namespace DevToolbox
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Must be the very first Application.* call. System (not the modern default of
            // PerMonitorV2) is deliberate, carried over from this app's old .NET Framework
            // App.config (which used System.Windows.Forms.ApplicationConfigurationSection's
            // DpiAwareness setting to the same effect - that mechanism isn't read on modern .NET,
            // this call is the direct equivalent): without any DPI awareness, Windows
            // bitmap-stretches the whole UI on a scaled display, blurring small text (button
            // labels) enough to misread. PerMonitorV2 isn't used because the HTML Viewer/Markdown
            // Previewer previews use the legacy WebBrowser control (WebView2 was tried and
            // reverted - its native loader DLL couldn't be made to travel inside the single
            // portable exe), which isn't itself per-monitor DPI aware - under PerMonitorV2 it lays
            // out content assuming more physical width than its WinForms container actually gets
            // on screen, running text past the visible edge with no scrollbar to reach it. Plain
            // System DPI awareness still renders the rest of the UI crisp at the monitor's scale,
            // without the per-monitor rescale events that desync the WebBrowser control - the
            // previews may still cut off wide content occasionally, a known limitation.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
