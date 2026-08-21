using System.Drawing.Drawing2D;
using DevToolbox.Core;
using DevToolbox.Tools.Documentation;

namespace DevToolbox.UI
{
    public class MainForm : Form
    {
        private Panel _navContainer = new();
        private Panel _navPanel = new();
        private Panel _contentPanel = new();
        private TextBox _txtNavSearch = new();
        private bool _searchShowingPlaceholder;
        private const string SearchPlaceholder = "Search tools... (Ctrl+K)";
        private Label _lblBreadcrumb = new();
        private Button _btnDocs = new();
        private Button _btnSettings = new();
        private readonly ToolTip _navToolTip = new();
        // A pinned tool renders in both the "Pinned" section and its normal category, so each
        // key can map to more than one Label - SelectTool needs to restyle every row for a tool,
        // not just whichever one happened to be added to the dictionary last.
        private readonly Dictionary<ITool, List<Label>> _navItems = new();
        private readonly AppSettings _settings = AppSettings.Load();
        private ITool? _activeTool;

        public MainForm()
        {
            Width = 1280;
            Height = 860;
            MinimumSize = new Size(1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            if (_settings.StartMaximized) WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            KeyPreview = true;
            KeyDown += OnFormKeyDown;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Fall back to the default form icon if the exe's embedded icon can't be read.
            }

            BuildUi();
        }

        private void OnFormKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.K)
            {
                _txtNavSearch.Focus();
                _txtNavSearch.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Oemcomma)
            {
                // Ctrl+, for Settings matches the convention several other apps (VS Code among
                // them) already use, rather than inventing a DevToolbox-specific binding.
                new SettingsForm(BuildUi).ShowDialog(this);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape && (_txtNavSearch.Focused || !_searchShowingPlaceholder))
            {
                // Clears an active filter from anywhere in the window, not just while the search
                // box itself has focus - e.SuppressKeyPress isn't set here, since Escape has no
                // other meaning to steal it from elsewhere in the main window.
                _txtNavSearch.Text = string.Empty;
                e.Handled = true;
            }
        }

        // Builds (or, after a theme toggle, rebuilds from scratch) the entire chrome. Every
        // control reads Theme.* colors at construction time, so tearing everything down and
        // rebuilding it is the simplest reliable way to re-theme the app - no per-control
        // "theme changed" event plumbing needed.
        private void BuildUi()
        {
            Controls.Clear();
            _navItems.Clear();

            Text = "DevToolbox";
            BackColor = Theme.Background;
            Font = Theme.BaseFont;

            var header = BuildHeader();

            _navContainer = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = Theme.NavBackground
            };
            _navContainer.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, _navContainer.Width - 1, 0, _navContainer.Width - 1, _navContainer.Height);
            };

            _navPanel = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.NavBackground,
                Padding = new Padding(0, 12, 0, 12),
                AutoScroll = true
            };

            var searchBar = BuildSearchBar();

            // Dock=Fill must be added before the Dock=Top sibling below it - see the docking
            // order note used throughout the tool controls.
            _navContainer.Controls.Add(_navPanel);
            _navContainer.Controls.Add(searchBar);

            _contentPanel = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(20)
            };

            Controls.Add(_contentPanel);
            Controls.Add(_navContainer);
            Controls.Add(header);

            RebuildNavList(string.Empty);

            // _activeTool wins when BuildUi() re-runs after a theme toggle (so it doesn't jump
            // back to the last-saved/default tool mid-session); _settings.LastTool wins on a
            // fresh launch, but only when "remember last-opened tool" is on; _settings.DefaultTool
            // is the configured fallback for when there's nothing remembered; the first tool in
            // the registry is the last resort if nothing else applies.
            var lastTool = _settings.RememberLastTool ? FindToolByName(_settings.LastTool) : null;
            var defaultTool = FindToolByName(_settings.DefaultTool);
            var toolToShow = _activeTool ?? lastTool ?? defaultTool ?? (ToolRegistry.All.Count > 0 ? ToolRegistry.All[0] : null);
            if (toolToShow is not null) SelectTool(toolToShow);
        }

        private Panel BuildSearchBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.NavBackground,
                Padding = new Padding(12, 8, 12, 8)
            };

            _txtNavSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = Theme.BaseFont,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Theme.Card,
                ForeColor = Theme.TextMuted,
                Text = SearchPlaceholder
            };
            _searchShowingPlaceholder = true;

            // net472 WinForms has no TextBox.PlaceholderText, so the placeholder is faked with
            // grayed-out text that's swapped for real (empty) input on focus, and restored on
            // blur if the user left it empty. _searchShowingPlaceholder guards TextChanged so
            // the placeholder text itself never gets treated as a search filter.
            _txtNavSearch.GotFocus += (_, _) =>
            {
                if (!_searchShowingPlaceholder) return;
                _searchShowingPlaceholder = false;
                _txtNavSearch.Text = string.Empty;
                _txtNavSearch.ForeColor = Theme.Text;
            };
            _txtNavSearch.LostFocus += (_, _) =>
            {
                if (_txtNavSearch.Text.Length > 0) return;
                _searchShowingPlaceholder = true;
                _txtNavSearch.ForeColor = Theme.TextMuted;
                _txtNavSearch.Text = SearchPlaceholder;
            };
            _txtNavSearch.TextChanged += (_, _) =>
            {
                if (_searchShowingPlaceholder) return;
                RebuildNavList(_txtNavSearch.Text.Trim());
            };
            _navToolTip.SetToolTip(_txtNavSearch, "Search tools (Ctrl+K)");
            bar.Controls.Add(_txtNavSearch);

            return bar;
        }

        private Panel BuildHeader()
        {
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.Card
            };
            headerPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "DevToolbox",
                Font = Theme.TitleFont,
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            _lblBreadcrumb = new Label
            {
                Font = Theme.BaseFont,
                ForeColor = Theme.TextMuted,
                UseMnemonic = false,
                AutoSize = true
            };

            // Icon-only, matching _btnSettings right next to it - a page/document glyph is
            // recognizable enough on its own that the "Documentation" label was redundant weight
            // in the header, and this keeps the two header buttons visually consistent with each
            // other instead of one being a wide labeled button and the other a plain icon.
            _btnDocs = new Button
            {
                Size = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Theme.Card,
                TabStop = false
            };
            _btnDocs.FlatAppearance.BorderSize = 0;
            _btnDocs.FlatAppearance.MouseOverBackColor = Theme.AccentSoft;
            _btnDocs.FlatAppearance.MouseDownBackColor = Theme.AccentSoft;
            _btnDocs.Paint += (_, e) => DrawDocumentIcon(e.Graphics, new Rectangle(6, 3, 24, 24), Theme.TextMuted);
            _navToolTip.SetToolTip(_btnDocs, "Documentation");
            _btnDocs.Click += (_, _) => new DocumentationForm().Show();

            _btnSettings = new Button
            {
                Size = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Theme.Card,
                TabStop = false
            };
            _btnSettings.FlatAppearance.BorderSize = 0;
            _btnSettings.FlatAppearance.MouseOverBackColor = Theme.AccentSoft;
            _btnSettings.FlatAppearance.MouseDownBackColor = Theme.AccentSoft;
            _btnSettings.Paint += (_, e) => DrawSettingsIcon(e.Graphics, new Rectangle(6, 3, 24, 24), Theme.TextMuted);
            _navToolTip.SetToolTip(_btnSettings, "Settings");
            // Theme (Light/Dark/System) lives inside the Settings dialog (see SettingsForm) rather
            // than as its own header button - BuildUi rebuilds the whole shell (including this
            // dialog's owner) whenever it's changed or reset in there.
            _btnSettings.Click += (_, _) => new SettingsForm(BuildUi).ShowDialog(this);

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(_lblBreadcrumb);
            headerPanel.Controls.Add(_btnDocs);
            headerPanel.Controls.Add(_btnSettings);

            headerPanel.Resize += (_, _) => PositionHeaderRightSide(headerPanel);
            PositionHeaderRightSide(headerPanel);

            return headerPanel;
        }

        private void PositionHeaderRightSide(Control header)
        {
            var docsX = header.Width - 20 - _btnDocs.Width;
            _btnDocs.Location = new Point(docsX, 13);

            var settingsX = docsX - 10 - _btnSettings.Width;
            _btnSettings.Location = new Point(settingsX, 13);

            _lblBreadcrumb.Location = new Point(settingsX - 20 - _lblBreadcrumb.Width, 20);
        }

        // A filled gear silhouette (body + teeth) with a background-colored ring cut out of the
        // middle and an accent-colored center dot - the classic "Settings" gear glyph, drawn with
        // GDI+ primitives rather than a bitmap/icon font so it scales cleanly and re-themes for
        // free (colors are read fresh on every repaint).
        // Three horizontal sliders with an offset knob each - the standard "preferences/settings"
        // glyph, matching CategoryIcons' hand-drawn line-art style rather than a bitmap/icon font.
        private static void DrawSettingsIcon(Graphics g, Rectangle b, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var brush = new SolidBrush(color);

            var rows = new[] { 0.2f, 0.5f, 0.8f };
            var knobPositions = new[] { 0.68f, 0.32f, 0.6f };
            var knobRadius = b.Width * 0.1f;

            for (var i = 0; i < rows.Length; i++)
            {
                var y = b.Top + b.Height * rows[i];
                g.DrawLine(pen, b.Left, y, b.Right, y);

                var knobX = b.Left + b.Width * knobPositions[i];
                g.FillEllipse(brush, knobX - knobRadius, y - knobRadius, knobRadius * 2, knobRadius * 2);
            }
        }

        // A page outline with a folded top-right corner and a few text lines - the standard
        // "document" glyph, matching CategoryIcons' hand-drawn line-art style.
        private static void DrawDocumentIcon(Graphics g, Rectangle b, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.4f) { LineJoin = LineJoin.Round };

            var foldX = b.Left + b.Width * 0.62f;
            var foldY = b.Top + b.Height * 0.32f;
            var pagePoints = new[]
            {
                new PointF(b.Left + b.Width * 0.14f, b.Top),
                new PointF(foldX, b.Top),
                new PointF(b.Right, foldY),
                new PointF(b.Right, b.Bottom),
                new PointF(b.Left + b.Width * 0.14f, b.Bottom)
            };
            g.DrawPolygon(pen, pagePoints);
            g.DrawLine(pen, foldX, b.Top, foldX, foldY);
            g.DrawLine(pen, foldX, foldY, b.Right, foldY);

            var lineRows = new[] { 0.55f, 0.7f, 0.85f };
            foreach (var row in lineRows)
            {
                var y = b.Top + b.Height * row;
                g.DrawLine(pen, b.Left + b.Width * 0.28f, y, b.Right - b.Width * 0.12f, y);
            }
        }

        // Tool names after which the nav adds a small extra gap, sub-grouping a category's
        // items the way freeformatter.com's own sidebar does (e.g. XML/JSON/HTML Validator +
        // XPath Tester read as one cluster, separate from the credit-card/regex/cron tools).
        private static readonly HashSet<string> ExtraGapAfter = new() { "XPath Tester" };

        private void RebuildNavList(string filter)
        {
            // Every category toggle clears and re-adds 60+ child controls one at a time.
            // SuspendLayout only defers *layout math* - it does nothing to stop the OS compositor
            // from visually updating the window while individual child controls are being
            // destroyed (Controls.Clear()) and recreated (each Controls.Add() one at a time) in
            // between, so a frame grabbed mid-rebuild can catch (and this was confirmed live, via
            // a frame-by-frame video capture) several category headers completely missing while
            // their tool rows and the rest of the list already reflect the new state - a ~60ms
            // flicker, not a persistent bug, but a real one. WM_SETREDRAW (suspended below via
            // NativeMethods, the same interop this app already uses for flicker-free RichTextBox
            // rebuilds) tells the OS to stop visually updating this window at all until told
            // otherwise, which is the only way to guarantee no such intermediate frame is ever
            // shown. A previous attempt at this combined WM_SETREDRAW with a *non-recursive*
            // resume-invalidate (NativeMethods.ResumeDrawing's default), which left child
            // Label/Panel controls never told to repaint afterward (a stale duplicate header, tool
            // rows with no text) - ResumeDrawing(recursive: true) below is the fix for that half of
            // it, not a reason to avoid WM_SETREDRAW altogether.
            //
            // Skipped entirely if the handle doesn't exist yet (the very first RebuildNavList call
            // happens inside BuildUi(), before the form has ever been shown) - checking
            // IsHandleCreated here (rather than just trying it) matters because reading the
            // .Handle property, which SuspendDrawing/ResumeDrawing do via SendMessage, has the side
            // effect of *forcing* handle creation if it doesn't exist yet; doing that before the
            // panel is even parented could create it with the wrong parent temporarily. That first
            // call also has nothing to fix (a freshly-built panel starts scrolled to the top
            // already, with nothing stale to flicker), so skipping it there is a no-op, not a
            // missed case.
            var canSuspendRedraw = _navPanel.IsHandleCreated;
            if (canSuspendRedraw) NativeMethods.SuspendDrawing(_navPanel);

            _navPanel.SuspendLayout();
            try
            {
                RebuildNavListCore(filter);
            }
            finally
            {
                _navPanel.ResumeLayout();

                // A category toggle/search can shrink the rebuilt content well below the panel's
                // previous scrolled-down position - AutoScrollPosition isn't automatically reclamped
                // to the new (shorter) extent, so the viewport was left showing blank background
                // above content that got squeezed down near the bottom. Every trigger for this
                // method (search, category toggle, pin toggle) is a case where snapping back to the
                // top is the expected, unsurprising behavior anyway. Note that selecting a tool
                // does *not* call this method (see SelectTool) - it only restyles the already-built
                // rows in place, specifically so that clicking around tools (which happens
                // constantly during normal use) never resets your scroll position or rebuilds 60+
                // controls just to record a Recently Used entry. That means Recently Used is
                // eventually consistent - it catches up next time the nav rebuilds for any other
                // reason (search, a category toggle, a pin, or the next launch) - rather than
                // live-updating on every single click.
                //
                // A plain assignment right here (immediately after ResumeLayout) turned out not to
                // be reliable on its own - ResumeLayout's own layout pass doesn't always finish
                // recalculating AutoScrollMinSize from the new child controls before this line
                // reads/writes the scroll position, so the assignment could silently clamp back
                // toward the *previous* (larger) scrolled-down offset instead of actually landing
                // on top - reproduced live as a reported bug (a category header rendered near the
                // bottom of the panel with the rest of the now-shorter content scrolled out of view
                // above it). Forcing an explicit PerformLayout first, resetting both the
                // AutoScrollPosition property *and* the underlying VerticalScroll value directly,
                // and then repeating the same reset once more (below) covers this reliably.
                _navPanel.PerformLayout();
                _navPanel.AutoScrollPosition = Point.Empty;
                _navPanel.VerticalScroll.Value = _navPanel.VerticalScroll.Minimum;

                if (canSuspendRedraw)
                {
                    NativeMethods.ResumeDrawing(_navPanel, recursive: true);
                }
                else
                {
                    _navPanel.Invalidate(true);
                    _navPanel.Update();
                }

                if (canSuspendRedraw)
                {
                    void ResetScroll()
                    {
                        _navPanel.AutoScrollPosition = Point.Empty;
                        _navPanel.VerticalScroll.Value = _navPanel.VerticalScroll.Minimum;
                    }

                    // Belt-and-suspenders: this bug came back after the migration to net10.0-windows
                    // even with the BeginInvoke pass below already in place - modern .NET's
                    // WinForms sometimes needs more than one message-loop iteration to finish
                    // settling AutoScrollMinSize after a Controls.Clear()+Add() churn like this one,
                    // so a single "wait one tick" BeginInvoke isn't watertight on its own. Reacting
                    // to the panel's own Layout event - which fires precisely when WinForms has just
                    // recalculated layout, AutoScrollMinSize included, rather than guessing how many
                    // ticks that takes - is the timing-independent version of the same fix. One-shot
                    // (unsubscribes itself) so it doesn't fight a deliberate user scroll afterwards.
                    void ResetOnNextLayout(object? s, LayoutEventArgs e)
                    {
                        _navPanel.Layout -= ResetOnNextLayout;
                        ResetScroll();
                    }
                    _navPanel.Layout += ResetOnNextLayout;

                    _navPanel.BeginInvoke(new Action(ResetScroll));
                }
            }
        }

        private void RebuildNavListCore(string filter)
        {
            _navPanel.Controls.Clear();
            _navItems.Clear();

            // Matches on Description too, not just Name - with 60+ tools, searching a concept
            // ("signed token", "slug") should find the relevant tool even if that word isn't
            // literally in its name.
            var matches = filter.Length == 0
                ? ToolRegistry.All
                : ToolRegistry.All.Where(t =>
                    t.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (_settings.SearchIncludesDescriptions && t.Description.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

            if (matches.Count == 0)
            {
                var lblNone = new Label
                {
                    Text = "No tools match your search.",
                    ForeColor = Theme.TextMuted,
                    Font = Theme.BaseFont,
                    AutoSize = false,
                    Location = new Point(16, 12),
                    Size = new Size(_navPanel.Width - 32, 40)
                };
                _navPanel.Controls.Add(lblNone);
                return;
            }

            var y = 6;

            // Reserve room for the vertical scrollbar in every child's width. Without this,
            // items sized off the pre-scrollbar panel width overhang by a few px once the
            // scrollbar actually appears, which triggers an unwanted horizontal scrollbar too.
            var scrollReserve = SystemInformation.VerticalScrollBarWidth + 4;

            // The Pinned section only makes sense against the full, unfiltered list - showing it
            // during a search would just duplicate whatever's already in the filtered results.
            if (filter.Length == 0)
            {
                var pinned = ToolRegistry.All.Where(t => _settings.IsPinned(t.Name)).ToList();
                if (pinned.Count > 0)
                {
                    y = RenderCategoryHeader("Pinned", y, scrollReserve);
                    if (!_settings.IsCategoryCollapsed("Pinned"))
                    {
                        foreach (var tool in pinned) y = RenderToolItem(tool, y, scrollReserve);
                        y += 10;
                    }
                }

                // OfType<ITool>() drops any recorded name that no longer resolves to a real tool
                // (e.g. one that's since been removed from ToolRegistry) rather than crashing or
                // rendering a blank row for it.
                var recent = _settings.RecentTools.Select(FindToolByName).OfType<ITool>().ToList();
                if (recent.Count > 0)
                {
                    y = RenderCategoryHeader("Recently Used", y, scrollReserve);
                    if (!_settings.IsCategoryCollapsed("Recently Used"))
                    {
                        foreach (var tool in recent) y = RenderToolItem(tool, y, scrollReserve);
                        y += 10;
                    }
                }
            }

            string? currentCategory = null;
            var currentCategoryCollapsed = false;
            foreach (var tool in matches)
            {
                if (tool.Category != currentCategory)
                {
                    if (currentCategory is not null) y += 10;
                    currentCategory = tool.Category;
                    currentCategoryCollapsed = _settings.IsCategoryCollapsed(currentCategory);
                    y = RenderCategoryHeader(currentCategory, y, scrollReserve);
                }

                // Collapse only hides browsing (empty filter) - while actively searching, every
                // match should stay visible regardless of which category it's filed under.
                if (currentCategoryCollapsed && filter.Length == 0) continue;

                y = RenderToolItem(tool, y, scrollReserve);

                // A couple of categories group tools into sub-clusters (matching
                // freeformatter.com's own sidebar) - a little extra breathing room after the
                // last item in a sub-cluster instead of a full new category header.
                if (ExtraGapAfter.Contains(tool.Name)) y += 10;
            }
        }

        /// <summary>Renders one collapsible category header (chevron + icon + label) at the given y and returns the y for the row after it. Clicking anywhere on the row toggles that category's collapsed state.</summary>
        private int RenderCategoryHeader(string category, int y, int scrollReserve)
        {
            var categoryForIcon = category; // fresh capture - see closure note below
            var collapsed = _settings.IsCategoryCollapsed(category);

            void Toggle()
            {
                _settings.ToggleCategoryCollapsed(category);
                RebuildNavList(_searchShowingPlaceholder ? string.Empty : _txtNavSearch.Text.Trim());
            }

            var headerPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(_navPanel.Width - scrollReserve, 26),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            headerPanel.Click += (_, _) => Toggle();
            headerPanel.Paint += (_, e) => DrawCollapseChevron(e.Graphics, new Rectangle(headerPanel.Width - 24, 6, 14, 14), Theme.NavCategoryText, collapsed);

            var iconPanel = new Panel
            {
                Location = new Point(16, 0),
                Size = new Size(22, 26),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            // Closures capture the *variable*, not a snapshot - without this local copy, every
            // icon's Paint handler would end up drawing whatever category happens to be by the
            // time painting actually runs (the last one assigned).
            iconPanel.Paint += (_, e) => CategoryIcons.Draw(e.Graphics, categoryForIcon, new Rectangle(2, 4, 18, 18), Theme.NavCategoryText);
            iconPanel.Click += (_, _) => Toggle();
            headerPanel.Controls.Add(iconPanel);

            var lblCategory = new Label
            {
                Text = category,
                UseMnemonic = false,
                ForeColor = Theme.NavCategoryText,
                Font = Theme.SectionFont,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Location = new Point(40, 0),
                Size = new Size(headerPanel.Width - 40 - 24, 26)
            };
            lblCategory.Click += (_, _) => Toggle();
            headerPanel.Controls.Add(lblCategory);

            _navPanel.Controls.Add(headerPanel);

            return y + 30;
        }

        // A small chevron - pointing right when collapsed, down when expanded - drawn to match
        // CategoryIcons' hand-drawn line-art style rather than a bitmap/icon font.
        private static void DrawCollapseChevron(Graphics g, Rectangle b, Color color, bool collapsed)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

            var cx = b.Left + b.Width / 2f;
            var cy = b.Top + b.Height / 2f;
            var half = b.Width * 0.3f;

            var points = collapsed
                ? new[] { new PointF(cx - half * 0.6f, cy - half), new PointF(cx + half * 0.6f, cy), new PointF(cx - half * 0.6f, cy + half) }
                : new[] { new PointF(cx - half, cy - half * 0.6f), new PointF(cx, cy + half * 0.6f), new PointF(cx + half, cy - half * 0.6f) };
            g.DrawLines(pen, points);
        }

        // Width of the pin-toggle glyph reserved at the right edge of every tool row.
        private const int PinColumnWidth = 22;

        /// <summary>Renders one tool's nav row (name + pin toggle) at the given y and returns the y for the row after it.</summary>
        private int RenderToolItem(ITool tool, int y, int scrollReserve)
        {
            var item = new Label
            {
                Text = tool.Name,
                UseMnemonic = false,
                Tag = tool,
                ForeColor = Theme.NavLinkText,
                Font = Theme.BaseFont,
                AutoSize = false,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(0, y),
                Size = new Size(_navPanel.Width - 16 - scrollReserve - PinColumnWidth, 26),
                Padding = new Padding(34, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            item.Click += (_, _) => SelectTool(tool);
            item.MouseEnter += (_, _) => { if (_activeTool != tool) item.ForeColor = Theme.NavLinkHover; };
            item.MouseLeave += (_, _) => { if (_activeTool != tool) item.ForeColor = Theme.NavLinkText; };
            item.Paint += (_, e) =>
            {
                if (_activeTool == tool)
                {
                    using var accentBrush = new SolidBrush(Theme.Accent);
                    e.Graphics.FillRectangle(accentBrush, 12, 4, 3, item.Height - 8);
                }
            };
            _navToolTip.SetToolTip(item, tool.Name);
            _navPanel.Controls.Add(item);

            if (!_navItems.TryGetValue(tool, out var rows))
            {
                rows = new List<Label>();
                _navItems[tool] = rows;
            }
            rows.Add(item);

            _navPanel.Controls.Add(BuildPinToggle(tool, y, scrollReserve));

            return y + 26;
        }

        /// <summary>Builds the star glyph that toggles a tool's pinned state, independent of the row's main click target.</summary>
        private Label BuildPinToggle(ITool tool, int y, int scrollReserve)
        {
            var isPinned = _settings.IsPinned(tool.Name);

            var star = new Label
            {
                Text = isPinned ? "★" : "☆", // filled / outline star
                Font = Theme.BaseFont,
                ForeColor = isPinned ? Theme.Warning : Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Location = new Point(_navPanel.Width - scrollReserve - PinColumnWidth, y),
                Size = new Size(PinColumnWidth, 26),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            _navToolTip.SetToolTip(star, isPinned ? "Unpin" : "Pin to top");

            star.Click += (_, _) =>
            {
                _settings.TogglePinned(tool.Name);
                RebuildNavList(_searchShowingPlaceholder ? string.Empty : _txtNavSearch.Text.Trim());
            };

            return star;
        }

        /// <summary>Finds a registered tool by its exact name, or null if none match (e.g. a saved name from a tool that was since removed).</summary>
        private static ITool? FindToolByName(string? name) =>
            name is null ? null : ToolRegistry.All.FirstOrDefault(t => t.Name == name);

        /// <summary>Marks the given tool active: restyles its nav item, updates the breadcrumb, and swaps the content panel to its view.</summary>
        private void SelectTool(ITool tool)
        {
            _activeTool = tool;
            if (_settings.RememberLastTool) _settings.SetLastTool(tool.Name);

            // Deliberately doesn't trigger a nav rebuild - see RebuildNavList's comment on why
            // Recently Used is eventually consistent rather than live-updating on every click.
            _settings.RecordRecentTool(tool.Name);

            foreach (var entry in _navItems)
            {
                var selected = entry.Key == tool;
                foreach (var label in entry.Value)
                {
                    label.BackColor = selected ? Theme.NavSelectedBackground : Color.Transparent;
                    label.ForeColor = selected ? Theme.NavSelectedText : Theme.NavLinkText;
                    label.Font = selected ? Theme.BoldFont : Theme.BaseFont;
                    label.Invalidate();
                }
            }

            _lblBreadcrumb.Text = $"{tool.Category} > {tool.Name}";
            PositionHeaderRightSide(_lblBreadcrumb.Parent ?? this);

            _contentPanel.SuspendLayout();
            _contentPanel.Controls.Clear();
            var view = tool.CreateView();
            view.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(view);
            _contentPanel.ResumeLayout();
        }
    }
}
