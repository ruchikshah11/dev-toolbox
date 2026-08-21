using DevToolbox.Tools.JsonFormatter;
using DevToolbox.UI;

namespace DevToolbox.Tools.DiffViewer
{
    public class DiffViewerControl : UserControl
    {
        private readonly TextBox _txtLeft = new();
        private readonly TextBox _txtRight = new();
        private readonly ComboBox _cboMode = new();
        private readonly Label _lblError = new();
        private readonly Label _lblSummary = new();
        private readonly RichTextBox _rtbDiff = new();

        public DiffViewerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it. Same-edge Dock=Top
            // siblings stack in reverse add-order, so the Original card (added last) ends up
            // visually on top, above Changed, above the options row, above the diff output.
            BuildOutputCard();
            BuildOptionsRow();
            BuildRightCard();
            BuildLeftCard();

            RunDiff();
        }

        private void BuildLeftCard()
        {
            var card = CardPanel.Add(this, "Original", 160);
            _txtLeft.Multiline = true;
            _txtLeft.ScrollBars = ScrollBars.Vertical;
            _txtLeft.AcceptsReturn = true;
            _txtLeft.AcceptsTab = true;
            _txtLeft.TextChanged += (_, _) => RunDiff();
            CardPanel.WrapWithBorder(card, _txtLeft, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
            AddChooseFileButton(card, () => LoadFileInto(_txtLeft));
        }

        private void BuildRightCard()
        {
            var card = CardPanel.Add(this, "Changed", 160);
            _txtRight.Multiline = true;
            _txtRight.ScrollBars = ScrollBars.Vertical;
            _txtRight.AcceptsReturn = true;
            _txtRight.AcceptsTab = true;
            _txtRight.TextChanged += (_, _) => RunDiff();
            CardPanel.WrapWithBorder(card, _txtRight, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
            AddChooseFileButton(card, () => LoadFileInto(_txtRight));
        }

        /// <summary>Adds a small "Choose File" button to the top-right of a card's title row - the same trick used for the Copy button in CardPanel.FillSplitPane.</summary>
        private static void AddChooseFileButton(Panel card, Action onClick)
        {
            var btn = new Button { Text = "Choose File", Size = new Size(110, 24) };
            Theme.StyleSecondaryButton(btn);
            btn.Click += (_, _) => onClick();
            card.Controls.Add(btn);

            void PositionButton() => btn.Location = new Point(card.Width - 18 - btn.Width, 10);
            card.Resize += (_, _) => PositionButton();
            PositionButton();
        }

        private static void LoadFileInto(TextBox target)
        {
            using var dialog = new OpenFileDialog { Title = "Choose a file" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            target.Text = File.ReadAllText(dialog.FileName);
        }

        private void BuildOptionsRow()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 10)
            };
            Controls.Add(bar);

            CardPanel.AddFieldLabel(bar, "Compare as", 18, 8);
            _cboMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboMode.Font = Theme.BaseFont;
            _cboMode.Location = new Point(100, 4);
            _cboMode.Width = 150;
            _cboMode.Items.AddRange(new object[] { "Plain Text", "JSON", "XML" });
            _cboMode.SelectedIndex = 0;
            _cboMode.SelectedIndexChanged += (_, _) => RunDiff();
            bar.Controls.Add(_cboMode);

            var btnSwap = new Button { Text = "Swap", Location = new Point(260, 4), Size = new Size(90, 26) };
            Theme.StyleSecondaryButton(btnSwap);
            btnSwap.Click += (_, _) =>
            {
                (_txtLeft.Text, _txtRight.Text) = (_txtRight.Text, _txtLeft.Text);
            };
            bar.Controls.Add(btnSwap);

            _lblError.Size = new Size(500, 24);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.TextAlign = ContentAlignment.MiddleRight;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);

            void PositionError() => _lblError.Location = new Point(bar.Width - 18 - _lblError.Width, 6);
            bar.Resize += (_, _) => PositionError();
            PositionError();
        }

        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "Diff", 0, fill: true);

            _lblSummary.AutoSize = false;
            _lblSummary.Font = Theme.BoldFont;
            _lblSummary.ForeColor = Theme.TextMuted;
            _lblSummary.Location = new Point(120, 12);
            _lblSummary.Size = new Size(300, 22);
            card.Controls.Add(_lblSummary);

            var btnCopy = new Button { Text = "Copy to Clipboard", Size = new Size(150, 28) };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (_, _) =>
            {
                if (_rtbDiff.TextLength > 0) Clipboard.SetText(_rtbDiff.Text);
            };
            card.Controls.Add(btnCopy);

            void PositionCopy() => btnCopy.Location = new Point(card.Width - 18 - btnCopy.Width, 8);
            card.Resize += (_, _) => PositionCopy();
            PositionCopy();

            _rtbDiff.ReadOnly = true;
            _rtbDiff.WordWrap = false;
            // Forced, not the auto Both - see the note in TabbedOutputView on why the automatic
            // horizontal scrollbar can't be relied on for WordWrap=false content set via code.
            _rtbDiff.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
            CardPanel.WrapWithBorder(card, _rtbDiff, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void RunDiff()
        {
            try
            {
                var diff = _cboMode.SelectedItem as string switch
                {
                    "JSON" => DiffViewerService.ComputeJsonDiff(_txtLeft.Text, _txtRight.Text),
                    "XML" => DiffViewerService.ComputeXmlDiff(_txtLeft.Text, _txtRight.Text),
                    _ => DiffViewerService.ComputeLineDiff(_txtLeft.Text, _txtRight.Text)
                };

                RenderDiff(diff);
                HideError();
            }
            catch (Exception ex)
            {
                _rtbDiff.Clear();
                _lblSummary.Text = string.Empty;
                ShowError(ex.Message);
            }
        }

        private void RenderDiff(List<DiffLine> lines)
        {
            var mode = _cboMode.SelectedItem as string;

            // Two-column line-number gutter (old | new), each padded to the widest number that
            // will actually appear - blank on whichever side a line doesn't exist on (Added has
            // no old position, Removed has no new one), matching the convention most diff tools
            // use so you can still tell where a change falls in each original document.
            var oldWidth = Math.Max(1, lines.Where(l => l.OldLineNumber.HasValue).Select(l => l.OldLineNumber!.Value).DefaultIfEmpty(0).Max().ToString().Length);
            var newWidth = Math.Max(1, lines.Where(l => l.NewLineNumber.HasValue).Select(l => l.NewLineNumber!.Value).DefaultIfEmpty(0).Max().ToString().Length);

            _rtbDiff.SuspendLayout();
            _rtbDiff.Clear();

            var added = 0;
            var removed = 0;
            foreach (var line in lines)
            {
                var (prefix, diffColor, background) = line.Kind switch
                {
                    DiffLineKind.Added => ("+ ", Theme.Success, Theme.SuccessSoft),
                    DiffLineKind.Removed => ("- ", Theme.Error, Theme.ErrorSoft),
                    _ => ("  ", Theme.Text, Theme.Card)
                };
                if (line.Kind == DiffLineKind.Added) added++;
                if (line.Kind == DiffLineKind.Removed) removed++;

                var gutter = $"{(line.OldLineNumber?.ToString() ?? "").PadLeft(oldWidth)} {(line.NewLineNumber?.ToString() ?? "").PadLeft(newWidth)}  ";

                var lineStart = _rtbDiff.TextLength;
                _rtbDiff.SelectionStart = lineStart;
                _rtbDiff.SelectionLength = 0;
                _rtbDiff.SelectionColor = Theme.TextMuted;
                _rtbDiff.AppendText(gutter);

                _rtbDiff.SelectionColor = diffColor;
                _rtbDiff.AppendText(prefix);

                // Colors the line's own text as JSON/XML syntax (matching every formatter's
                // output) instead of a single flat color - previously an unchanged line (the
                // vast majority of most diffs) rendered as plain, uncolored text even when
                // comparing JSON/XML, unlike every other output pane in this app.
                AppendLineText(line.Text, mode);
                _rtbDiff.AppendText("\n");

                _rtbDiff.Select(lineStart, _rtbDiff.TextLength - lineStart);
                _rtbDiff.SelectionBackColor = background;
            }

            _rtbDiff.SelectionStart = 0;
            _rtbDiff.SelectionLength = 0;
            _rtbDiff.ResumeLayout();

            // SuspendLayout/ResumeLayout only defer child-control layout math - they don't force a
            // repaint. Confirmed directly: after a re-run, _rtbDiff's real internal Text was
            // correctly updated (even correctly empty for "no differences"), but the on-screen
            // control kept showing a stale render from a previous, unrelated diff. Invalidate()
            // alone only *schedules* a repaint for whenever the message loop gets to it; Update()
            // forces it to happen synchronously, right now - the same fix already applied
            // elsewhere in this app (NativeMethods.ResumeDrawing) for the same class of bug.
            _rtbDiff.Invalidate();
            _rtbDiff.Update();

            _lblSummary.Text = added == 0 && removed == 0 ? "No differences." : $"+{added} added, -{removed} removed";
        }

        /// <summary>Appends one diff line's text (no prefix, no trailing newline), colored token-by-token to match the active compare mode, or plain if there's no tokenizer for it.</summary>
        private void AppendLineText(string text, string? mode)
        {
            if (text.Length == 0) return;

            switch (mode)
            {
                case "JSON":
                    foreach (var segment in JsonInputColorizer.BuildSegments(text))
                    {
                        if (segment.Text.Length == 0) continue;
                        var start = _rtbDiff.TextLength;
                        _rtbDiff.AppendText(segment.Text);
                        _rtbDiff.Select(start, segment.Text.Length);
                        _rtbDiff.SelectionColor = JsonColors.For(segment.Kind);
                    }
                    break;

                case "XML":
                    foreach (var segment in MarkupSyntaxTokenizer.Tokenize(text))
                    {
                        if (segment.Text.Length == 0) continue;
                        var start = _rtbDiff.TextLength;
                        _rtbDiff.AppendText(segment.Text);
                        _rtbDiff.Select(start, segment.Text.Length);
                        _rtbDiff.SelectionColor = MarkupSyntaxColors.For(segment.Kind);
                    }
                    break;

                default:
                    _rtbDiff.AppendText(text);
                    break;
            }
        }

        private void ShowError(string message)
        {
            _lblError.Text = message;
            _lblError.Visible = true;
        }

        private void HideError() => _lblError.Visible = false;
    }
}
