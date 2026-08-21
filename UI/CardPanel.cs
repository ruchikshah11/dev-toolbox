using System.Drawing.Drawing2D;

namespace DevToolbox.UI
{
    // Shared "white card with a titled border" building block, matching the visual language
    // used across the DevToolbox tools (and SPDummyDataGenerator before it).
    internal static class CardPanel
    {
        public static Panel Add(Control parent, string title, int height, bool fill = false)
        {
            var slot = new Panel
            {
                Dock = fill ? DockStyle.Fill : DockStyle.Top,
                Height = height,
                BackColor = Theme.Background,
                Padding = fill ? new Padding(0) : new Padding(0, 0, 0, 14)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Card,
                Padding = new Padding(18, 14, 18, 14)
            };
            card.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            slot.Controls.Add(card);
            parent.Controls.Add(slot);

            if (!string.IsNullOrEmpty(title))
            {
                var lblTitle = new Label
                {
                    Text = title,
                    Font = Theme.SectionFont,
                    ForeColor = Theme.Text,
                    AutoSize = true,
                    Location = new Point(18, 12)
                };
                card.Controls.Add(lblTitle);
            }

            return card;
        }

        public static Label AddFieldLabel(Control parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = Theme.TextMuted,
                Font = Theme.BoldFont,
                AutoSize = true,
                Location = new Point(x, y)
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        // Neither BorderStyle.FixedSingle nor Fixed3D render a full, consistent border across
        // Windows themes, so the border is drawn manually the same way the cards are.
        public static Panel WrapWithBorder(Control parent, TextBoxBase input, Point location, int width, int height, AnchorStyles anchor)
        {
            var wrapper = new Panel
            {
                Location = location,
                Anchor = anchor,
                Width = width,
                Height = height,
                BackColor = Theme.Card,
                Padding = new Padding(1)
            };
            wrapper.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, wrapper.Width - 1, wrapper.Height - 1);
            };

            input.BorderStyle = BorderStyle.None;
            input.Font = Theme.MonoFont;
            input.BackColor = Theme.Card;
            input.ForeColor = Theme.Text;
            input.Dock = DockStyle.Fill;
            wrapper.Controls.Add(input);
            parent.Controls.Add(wrapper);

            // parent.Width/Height at call time may not be its real, final laid-out size yet -
            // this control can be constructed before it's actually part of the visible form. The
            // exact same stale-pre-layout-size bug was confirmed and fixed for
            // CardPanel.AddSplitView's SplitContainer (a wrong-too-small size that Anchor alone
            // never corrected once the real size became available, leaving wrapped text/scroll
            // computed against the wrong width and visibly clipped). Every caller here already
            // expresses width/height as "card.Width - 36" etc., i.e. a fixed margin from parent's
            // size at that moment - preserving that margin and reapplying it against parent's
            // CURRENT size on every resize, rather than trusting Anchor to grow the wrapper
            // correctly on its own, is what actually keeps this correct.
            var growsWidth = (anchor & AnchorStyles.Right) != 0;
            var growsHeight = (anchor & AnchorStyles.Bottom) != 0;
            var rightMargin = parent.Width - width - location.X;
            var bottomMargin = parent.Height - height - location.Y;

            void SyncBounds()
            {
                if (growsWidth) wrapper.Width = Math.Max(1, parent.Width - location.X - rightMargin);
                if (growsHeight) wrapper.Height = Math.Max(1, parent.Height - location.Y - bottomMargin);
            }
            parent.Resize += (_, _) => SyncBounds();
            SyncBounds();

            return wrapper;
        }

        public static ComboBox MakeDropdown(Control parent, int x, int y, int width)
        {
            var combo = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.BaseFont
            };
            parent.Controls.Add(combo);
            return combo;
        }

        // Shared "your input on the left, result on the right" scaffolding used by every
        // formatter/converter/validator-style tool (JsonFormatterControl, TextTransformControl,
        // ...) - kept in one place so a future layout tweak (splitter width, pane title styling,
        // initial sizing) applies everywhere at once instead of being hand-copied per tool.

        /// <summary>Builds a resizable, side-by-side split view filling the given fill card.</summary>
        public static SplitContainer AddSplitView(Panel card)
        {
            var split = new SplitContainer
            {
                // card.Width/Height can still be small (not yet the real laid-out size) at this
                // point, so the initial Size is floored well above Panel1MinSize + Panel2MinSize
                // to guarantee SplitterDistance below is always a valid assignment.
                Location = new Point(18, 42),
                Size = new Size(Math.Max(420, card.Width - 36), Math.Max(420, card.Height - 58)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                BackColor = Theme.Border,
                Panel1MinSize = 200,
                Panel2MinSize = 200
            };
            card.Controls.Add(split);

            void PositionSplitter()
            {
                if (split.Width < split.Panel1MinSize + split.Panel2MinSize) return;
                split.SplitterDistance = split.Width / 2;
            }

            // Anchor alone turned out not to reliably resize this SplitContainer once card's real
            // (final, laid-out) size becomes available - confirmed directly by inspecting a live
            // running instance, where the SplitContainer was still sized against card's small,
            // pre-layout width, ending up WIDER than card itself and clipping its own contents
            // (including word-wrapped output text, which wraps against that wrong-too-wide size).
            // Explicitly re-syncing Bounds from card's current size on every card resize - rather
            // than trusting Anchor's baseline - is what actually keeps this correct.
            void SyncBounds()
            {
                split.SetBounds(18, 42, Math.Max(420, card.Width - 36), Math.Max(420, card.Height - 58));
                PositionSplitter();
            }
            card.Resize += (_, _) => SyncBounds();
            split.Resize += (_, _) => PositionSplitter();
            SyncBounds();

            return split;
        }

        /// <summary>
        /// Fills one split pane with a Dock=Fill content control plus a thin section-title header
        /// above it - and, if <paramref name="onCopy"/> is given, a Copy icon button in that same
        /// header row. <paramref name="content"/> is docked Fill here, so callers should leave
        /// its Dock unset. Plain textboxes get their theme colors applied here too, since that
        /// used to be set per-tool before this scaffolding was centralized.
        /// </summary>
        public static void FillSplitPane(Panel host, string title, Control content, Action? onCopy = null)
        {
            host.BackColor = Theme.Card;

            if (content is TextBoxBase textBox)
            {
                textBox.BorderStyle = BorderStyle.None;
                textBox.BackColor = Theme.Card;
                textBox.ForeColor = Theme.Text;
            }

            content.Dock = DockStyle.Fill;
            host.Controls.Add(content);

            // A real Panel, not a Label, hosts the title text and Copy button - a Label isn't a
            // proper WinForms container, and child controls added to one (the Copy button,
            // previously) can silently get painted over instead of reliably showing.
            var titleBar = new Panel
            {
                BackColor = Theme.Background,
                Dock = DockStyle.Top,
                Height = 30
            };

            var lblTitle = new Label
            {
                Text = title.ToUpperInvariant(),
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = Theme.SectionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Background,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            titleBar.Controls.Add(lblTitle);

            if (onCopy is not null)
            {
                var btnCopy = new Button
                {
                    Size = new Size(28, 24),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    BackColor = Theme.Background,
                    TabStop = false
                };
                btnCopy.FlatAppearance.BorderSize = 0;
                btnCopy.FlatAppearance.MouseOverBackColor = Theme.AccentSoft;
                btnCopy.FlatAppearance.MouseDownBackColor = Theme.AccentSoft;
                btnCopy.Paint += (_, e) => DrawCopyIcon(e.Graphics, new Rectangle(5, 3, 18, 18), Theme.TextMuted);
                btnCopy.Click += (_, _) =>
                {
                    onCopy();
                    // A separate ToolTip instance from CopyTooltip - that one is bound via
                    // SetToolTip for the persistent hover label, and reusing it for this transient
                    // one-shot confirmation would fight with that binding.
                    CopiedToast.Show("Copied to clipboard", btnCopy, btnCopy.Width / 2, btnCopy.Height + 6, 1500);
                };
                CopyTooltip.SetToolTip(btnCopy, "Copy to Clipboard");
                titleBar.Controls.Add(btnCopy);

                // lblTitle (Dock=Fill, opaque background, added to titleBar first) sits in FRONT
                // of btnCopy in z-order by default - the first control added to a Controls
                // collection ends up frontmost, not backmost - so without this, lblTitle's Fill
                // area completely paints over btnCopy and the copy button is invisible even
                // though it exists, is positioned correctly, and is fully clickable/functional.
                // Confirmed directly via GetWindow(GW_CHILD)/GW_HWNDNEXT on a live instance.
                btnCopy.BringToFront();

                void PositionCopy() => btnCopy.Location = new Point(titleBar.Width - 8 - btnCopy.Width, 3);
                titleBar.Resize += (_, _) => PositionCopy();

                // Added first so Dock=Top assigns its real (parent-width) size before the
                // initial PositionCopy() runs.
                host.Controls.Add(titleBar);
                PositionCopy();
            }
            else
            {
                host.Controls.Add(titleBar);
            }
        }

        // Shared so every FillSplitPane copy button reuses the same tooltip instance rather than
        // each one allocating its own ToolTip component.
        private static readonly ToolTip CopyTooltip = new();

        // Shows the transient "Copied to clipboard" confirmation after a click - kept separate
        // from CopyTooltip, which stays bound (via SetToolTip) to the persistent hover label.
        private static readonly ToolTip CopiedToast = new();

        // Two overlapping outlined squares (the standard "copy" glyph) - drawn rather than a
        // bitmap/icon font so it re-themes and scales for free, matching CategoryIcons' style.
        private static void DrawCopyIcon(Graphics g, Rectangle b, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.3f) { LineJoin = LineJoin.Round };

            var size = b.Width * 0.66f;
            var backRect = new RectangleF(b.Left, b.Top, size, size);
            var frontRect = new RectangleF(b.Right - size, b.Bottom - size, size, size);

            g.DrawRectangle(pen, backRect.X, backRect.Y, backRect.Width, backRect.Height);

            // Erases the corner of the back square that the front one overlaps, so the two read
            // as separate sheets rather than one merged shape - filled with the button's own
            // resting background, since that's what's actually behind this icon.
            using (var eraseBrush = new SolidBrush(Theme.Background))
            {
                g.FillRectangle(eraseBrush, frontRect.X - 1, frontRect.Y - 1, frontRect.Width + 2, frontRect.Height + 2);
            }

            g.DrawRectangle(pen, frontRect.X, frontRect.Y, frontRect.Width, frontRect.Height);
        }
    }
}
