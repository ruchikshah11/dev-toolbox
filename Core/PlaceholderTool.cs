using DevToolbox.UI;

namespace DevToolbox.Core
{
    // One reusable "not built yet" tool/view so the full freeformatter-style navigation tree
    // can be wired up immediately, with each entry ready to swap for a real ITool later.
    public sealed class PlaceholderTool : ITool
    {
        public PlaceholderTool(string category, string name, string description)
        {
            Category = category;
            Name = name;
            Description = description;
        }

        public string Category { get; }
        public string Name { get; }
        public string Description { get; }

        public Control CreateView() => new PlaceholderControl(Name, Description);
    }

    internal sealed class PlaceholderControl : UserControl
    {
        public PlaceholderControl(string name, string description)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            var card = CardPanel.Add(this, "", 0, fill: true);

            var lblBadge = new Label
            {
                Text = "COMING SOON",
                Font = new Font("Segoe UI Semibold", 8f),
                ForeColor = Theme.Warning,
                BackColor = ColorTranslator.FromHtml("#FEF3E2"),
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                Location = new Point(18, 16)
            };
            card.Controls.Add(lblBadge);

            var lblTitle = new Label
            {
                Text = name,
                UseMnemonic = false,
                Font = Theme.TitleFont,
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(18, 46)
            };
            card.Controls.Add(lblTitle);

            var lblDescription = new Label
            {
                Text = description,
                Font = Theme.BaseFont,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Location = new Point(18, 84),
                Size = new Size(560, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(lblDescription);

            var lblNote = new Label
            {
                Text = "This tool isn't implemented in DevToolbox yet - it's wired into the navigation and ready to build next.",
                Font = Theme.BaseFont,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Location = new Point(18, 150),
                Size = new Size(560, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(lblNote);
        }
    }
}
