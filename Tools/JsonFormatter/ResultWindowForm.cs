using DevToolbox.UI;
using Newtonsoft.Json.Linq;

namespace DevToolbox.Tools.JsonFormatter
{
    // Full-size view opened by "Format JSON to New Window" - reuses JsonOutputView so the
    // text/tree rendering logic isn't duplicated between the embedded and pop-out views.
    public class ResultWindowForm : Form
    {
        public ResultWindowForm(string title, List<JsonSegment> segments, JToken rootToken)
        {
            Text = title;
            Width = 900;
            Height = 700;
            MinimumSize = new Size(500, 350);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            Font = Theme.BaseFont;

            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Card
            };
            toolbar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            var btnCopy = new Button
            {
                Text = "Copy to Clipboard",
                Location = new Point(16, 9),
                Size = new Size(150, 30)
            };
            Theme.StyleSecondaryButton(btnCopy);
            var formattedText = string.Concat(segments.Select(s => s.Text));
            btnCopy.Click += (_, _) => Clipboard.SetText(formattedText);
            toolbar.Controls.Add(btnCopy);

            var output = new JsonOutputView();

            Controls.Add(output);
            Controls.Add(toolbar);

            output.Render(segments, rootToken);
        }
    }
}
