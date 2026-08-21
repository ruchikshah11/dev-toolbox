using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.Documentation
{
    // A "List of tools" reference page in the style of freeformatter.com's own tool-list pages:
    // every tool gets its own bordered card with a heading, description and bullet list. Built
    // as real WinForms controls (not a RichTextBox text dump) so it matches the rest of the
    // app's card-based look - and so long descriptions/bullets wrap and space out properly
    // instead of reading as one dense block of text.
    public class DocumentationForm : Form
    {
        private const int ContentWidth = 760;
        private const int MinMargin = 28;
        private readonly Panel _scroll = new();

        // The margin actually used the last time content was laid out - tracked so a later
        // resize/maximize can re-center by shifting existing controls rather than rebuilding the
        // whole page from scratch.
        private int _marginX;

        public DocumentationForm()
        {
            Text = "DevToolbox - List of Tools";
            Width = 940;
            Height = 820;
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            Font = Theme.BaseFont;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Fall back to the default form icon if the exe's embedded icon can't be read.
            }

            _scroll.Dock = DockStyle.Fill;
            _scroll.AutoScroll = true;
            _scroll.BackColor = Theme.Background;
            _scroll.Padding = new Padding(0, 0, 0, 30);
            Controls.Add(_scroll);

            Render();

            // Maximizing (or otherwise resizing) this dialog previously left the fixed-width
            // content pinned to the left edge with a large dead area on the right - re-center it
            // instead of rebuilding the whole page every time the window changes size.
            _scroll.Resize += (_, _) => RecenterContent();
        }

        /// <summary>Computes the left margin that centers the fixed-width content column within the scroll panel's current width, never going below MinMargin.</summary>
        private int ComputeMarginX() => Math.Max(MinMargin, (_scroll.ClientSize.Width - ContentWidth) / 2);

        /// <summary>Shifts every already-rendered control horizontally so the content stays centered after the window is resized/maximized.</summary>
        private void RecenterContent()
        {
            var newMarginX = ComputeMarginX();
            if (newMarginX == _marginX) return;

            var deltaX = newMarginX - _marginX;
            _marginX = newMarginX;

            foreach (Control control in _scroll.Controls)
            {
                control.Left += deltaX;
            }
        }

        private void Render()
        {
            _marginX = ComputeMarginX();
            var marginX = _marginX;
            var y = 28;

            var lblTitle = new Label
            {
                Text = "List of Tools",
                Font = new Font("Segoe UI Semibold", 20f),
                ForeColor = Theme.Text,
                AutoSize = true
            };
            _scroll.Controls.Add(lblTitle);
            lblTitle.Location = new Point(marginX + (ContentWidth - lblTitle.Width) / 2, y);
            y += lblTitle.Height + 2;

            var lblByline = new Label
            {
                Text = "Created By Ruchik Shah",
                Font = Theme.BaseFont,
                ForeColor = Theme.TextMuted,
                AutoSize = true
            };
            _scroll.Controls.Add(lblByline);
            lblByline.Location = new Point(marginX + (ContentWidth - lblByline.Width) / 2, y);
            y += lblByline.Height + 32;

            // Filled in as the category headers below are laid out, then read back by the TOC
            // links' click handlers - both run in this same Render() call, and closures capture
            // the dictionary by reference, so by the time a user can actually click a link every
            // entry is already populated.
            var categoryY = new Dictionary<string, int>();
            y += RenderTableOfContents(marginX, y, categoryY) + 24;

            string? currentCategory = null;
            foreach (var tool in ToolRegistry.All)
            {
                if (tool.Category != currentCategory)
                {
                    if (currentCategory is not null) y += 12;
                    currentCategory = tool.Category;
                    categoryY[currentCategory] = y;

                    var iconPanel = new Panel
                    {
                        Location = new Point(marginX, y),
                        Size = new Size(22, 22),
                        BackColor = Color.Transparent
                    };
                    var categoryForIcon = currentCategory;
                    iconPanel.Paint += (_, e) => CategoryIcons.Draw(e.Graphics, categoryForIcon, new Rectangle(2, 2, 18, 18), Theme.TextMuted);
                    _scroll.Controls.Add(iconPanel);

                    var lblCategory = new Label
                    {
                        Text = currentCategory.ToUpperInvariant(),
                        UseMnemonic = false,
                        Font = new Font("Segoe UI Semibold", 11f),
                        ForeColor = Theme.TextMuted,
                        AutoSize = true,
                        Location = new Point(marginX + 28, y + 2)
                    };
                    _scroll.Controls.Add(lblCategory);
                    y += 26;

                    var divider = new Panel
                    {
                        Location = new Point(marginX, y),
                        Size = new Size(ContentWidth, 1),
                        BackColor = Theme.Border
                    };
                    _scroll.Controls.Add(divider);
                    y += 16;
                }

                y += RenderToolCard(tool, marginX, y) + 16;
            }
        }

        /// <summary>Renders a two-column list of category links that jump the scroll position to that category's header.</summary>
        private int RenderTableOfContents(int x, int y, Dictionary<string, int> categoryY)
        {
            var categories = ToolRegistry.All.Select(t => t.Category).Distinct().ToList();

            var card = new Panel
            {
                Location = new Point(x, y),
                Width = ContentWidth,
                BackColor = Theme.Card
            };
            card.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            const int pad = 20;
            var innerY = 16;

            var lblHeading = new Label
            {
                Text = "CONTENTS",
                UseMnemonic = false,
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(pad, innerY)
            };
            card.Controls.Add(lblHeading);
            innerY += lblHeading.Height + 10;

            // Two columns keep this compact given ~13 categories - one long single column would
            // push the actual tool cards further down the page for no real benefit.
            var columnWidth = (ContentWidth - pad * 2 - 24) / 2;
            var colY = new[] { innerY, innerY };

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var col = i % 2;

                var link = new Label
                {
                    Text = category,
                    UseMnemonic = false,
                    Font = Theme.BoldFont,
                    ForeColor = Theme.NavLinkText,
                    AutoSize = true,
                    Cursor = Cursors.Hand,
                    Location = new Point(pad + col * (columnWidth + 24), colY[col])
                };
                // Fresh capture per iteration - without this every link's Click handler would
                // close over the same loop variable and jump to whichever category happened to
                // be last by the time it's clicked (same pitfall noted in MainForm's nav list).
                var categoryForClick = category;
                link.Click += (_, _) =>
                {
                    if (categoryY.TryGetValue(categoryForClick, out var targetY)) _scroll.AutoScrollPosition = new Point(0, targetY);
                };
                link.MouseEnter += (_, _) => link.ForeColor = Theme.NavLinkHover;
                link.MouseLeave += (_, _) => link.ForeColor = Theme.NavLinkText;
                card.Controls.Add(link);

                colY[col] += link.Height + 6;
            }

            card.Height = Math.Max(colY[0], colY[1]) + 16;
            _scroll.Controls.Add(card);
            return card.Height;
        }

        private int RenderToolCard(ITool tool, int x, int y)
        {
            var doc = ToolHighlights.For(tool.Name);

            var card = new Panel
            {
                Location = new Point(x, y),
                Width = ContentWidth,
                BackColor = Theme.Card
            };
            card.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            const int pad = 20;
            var innerY = 16;

            var lblName = new Label
            {
                Text = doc?.DisplayName ?? tool.Name,
                UseMnemonic = false,
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(pad, innerY)
            };
            card.Controls.Add(lblName);
            innerY += lblName.Height + 8;

            var lblDesc = new Label
            {
                Text = doc?.Description ?? tool.Description,
                UseMnemonic = false,
                Font = Theme.BaseFont,
                ForeColor = Theme.Text,
                AutoSize = true,
                MaximumSize = new Size(ContentWidth - pad * 2, 0),
                Location = new Point(pad, innerY)
            };
            card.Controls.Add(lblDesc);
            innerY += lblDesc.Height + (doc?.Bullets.Length > 0 ? 12 : 0);

            foreach (var bullet in doc?.Bullets ?? Array.Empty<string>())
            {
                var lblBullet = new Label
                {
                    Text = "•   " + bullet,
                    UseMnemonic = false,
                    Font = Theme.BaseFont,
                    ForeColor = Theme.TextMuted,
                    AutoSize = true,
                    MaximumSize = new Size(ContentWidth - pad * 2 - 16, 0),
                    Location = new Point(pad + 16, innerY)
                };
                card.Controls.Add(lblBullet);
                innerY += lblBullet.Height + 7;
            }

            card.Height = innerY + 16;
            _scroll.Controls.Add(card);
            return card.Height;
        }
    }
}
