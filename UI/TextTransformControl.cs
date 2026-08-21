using System.Text;
using DevToolbox.Core;

namespace DevToolbox.UI
{
    // One named operation offered by a TextTransformControl (e.g. "Encode" / "Decode",
    // "Uppercase" / "Lowercase" / "Reverse", ...). Transform receives the raw input textbox
    // text and returns what to show in the output box, or throws to surface a validation
    // error (caught and shown in the error label).
    public readonly record struct TextTransformAction(string Label, Func<string, string> Transform, bool Primary = false);

    // What, if anything, the input pane should be colorized as - the output pane is left plain
    // regardless, since most tools' output either isn't the same shape as the input (e.g. a
    // validator's result mixes prose and data) or is already handled by a dedicated output view
    // (JsonFormatterControl). Defaults to PlainText so the ~30 unrelated encoder/converter/
    // utility tools built on this control are unaffected.
    public enum TextTransformContentKind { PlainText, Json, Markup }

    /// <summary>
    /// Generic "paste text (or upload a file) -> run one of N operations -> see the result"
    /// shell shared by every tool whose job is a pure string-in/string-out transform:
    /// encoders/decoders, escapers, format converters, string utilities, etc. Your input and the
    /// result sit side by side (matching JsonFormatterControl's split-view convention) so both
    /// are visible at once, with upload/action controls condensed into compact bars on top.
    /// Keeping this in one place means each tool only has to supply its transform functions, not
    /// rebuild the input/output/upload/error UI.
    /// </summary>
    public class TextTransformControl : UserControl
    {
        // RichTextBox, not TextBox, so the input can be colorized when contentKind opts into it
        // (see BuildSplitView) - behaves identically to TextBox for the PlainText tools that
        // don't.
        private readonly RichTextBox _input = new();
        private readonly RichTextBox _output = new();
        private readonly Label _lblError = new();
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly ComboBox _cboEncoding = new();

        private string? _uploadedFilePath;

        // Whichever action button was clicked most recently. Once set, further typing re-runs
        // it automatically (see OnInputChanged) so results like Stats stay live instead of
        // requiring a re-click after every edit.
        private TextTransformAction? _lastAction;

        // The button currently styled as active (see SetActiveButton) - kept separate from
        // _lastAction because the default-highlighted button at startup hasn't been run yet.
        private Button? _activeButton;

        // Null (the default) leaves the output styled as plain text, correct for the ~30
        // encoder/converter/utility tools where a result isn't a pass/fail. A validator tool
        // passes a classifier instead, so its result colors green (Theme.Success) or red
        // (Theme.Error) - see ApplyResultStyle.
        private readonly Func<string, bool>? _isSuccessResult;

        // useDropdownSelector: true swaps the button row for a single "Operation" dropdown -
        // better once a tool has enough actions that a button row would wrap across several
        // rows (see String Utilities' 11 actions). Tools with just 2-4 actions (most
        // encoders/converters) stay as buttons, since one-click toggling beats a dropdown there.
        public TextTransformControl(string inputTitle, string outputTitle, IReadOnlyList<TextTransformAction> actions, string initialInput = "", bool useDropdownSelector = false, TextTransformContentKind contentKind = TextTransformContentKind.PlainText, Func<string, bool>? isSuccessResult = null, TextTransformContentKind outputContentKind = TextTransformContentKind.PlainText)
        {
            _isSuccessResult = isSuccessResult;
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top bars below it - see the docking order
            // note in MainForm/JsonFormatterControl. Bars are then added bottom-most-first (the
            // upload bar, added last, ends up above the action bar/dropdown, itself above the
            // fill card) - same reverse-stacking convention used throughout this app.
            var card = CardPanel.Add(this, $"{inputTitle} → {outputTitle}", 0, fill: true);
            BuildSplitView(card, inputTitle, outputTitle, contentKind, outputContentKind);

            if (useDropdownSelector) BuildActionDropdown(actions);
            else BuildActionBar(actions);
            BuildUploadBar();

            _input.Text = initialInput;
            _input.TextChanged += OnInputChanged;
        }

        private void OnInputChanged(object? sender, EventArgs e)
        {
            if (_lastAction is { } action) RunAction(action);
        }

        /// <summary>Builds the compact upload bar: Choose File, the chosen file's name, and the encoding dropdown used to decode it.</summary>
        private void BuildUploadBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 10)
            };
            Controls.Add(bar);

            _btnChooseFile.Text = "Choose File";
            _btnChooseFile.Location = new Point(18, 4);
            _btnChooseFile.Size = new Size(110, 30);
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += OnChooseFileClick;
            bar.Controls.Add(_btnChooseFile);

            _lblFileName.Text = "No file chosen";
            _lblFileName.ForeColor = Theme.TextMuted;
            _lblFileName.Font = Theme.BaseFont;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Location = new Point(136, 8);
            _lblFileName.Size = new Size(220, 22);
            bar.Controls.Add(_lblFileName);

            AddInlineLabel(bar, "Encoding:", 372, 14);
            _cboEncoding.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboEncoding.Font = Theme.BaseFont;
            _cboEncoding.Location = new Point(440, 8);
            _cboEncoding.Width = 200;
            foreach (var (display, encoding) in EncodingCatalog.Available)
            {
                _cboEncoding.Items.Add(new EncodingItem(display, encoding));
            }
            if (_cboEncoding.Items.Count > 0) _cboEncoding.SelectedIndex = 0;
            _cboEncoding.SelectedIndexChanged += (_, _) => { if (_uploadedFilePath is not null) LoadUploadedFile(); };
            bar.Controls.Add(_cboEncoding);
        }

        /// <summary>Adds a small bold muted label at the given position - a compact, inline-left field label used to pack more controls per row.</summary>
        private static void AddInlineLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = Theme.BoldFont,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(x, y)
            });
        }

        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose a file",
                Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _uploadedFilePath = dialog.FileName;
            _lblFileName.Text = Path.GetFileName(_uploadedFilePath);
            LoadUploadedFile();
        }

        private void LoadUploadedFile()
        {
            try
            {
                var bytes = File.ReadAllBytes(_uploadedFilePath!);
                var encoding = (_cboEncoding.SelectedItem as EncodingItem)?.Encoding ?? EncodingCatalog.Default;
                _input.Text = encoding.GetString(bytes);
                HideError();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowError($"Could not read file: {ex.Message}");
            }
        }

        private sealed class EncodingItem
        {
            public EncodingItem(string display, Encoding encoding)
            {
                Display = display;
                Encoding = encoding;
            }

            public string Display { get; }
            public Encoding Encoding { get; }
            public override string ToString() => Display;
        }

        /// <summary>Builds a single "Operation" dropdown in place of the button row - see the useDropdownSelector note on the constructor.</summary>
        private void BuildActionDropdown(IReadOnlyList<TextTransformAction> actions)
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 14)
            };
            Controls.Add(bar);

            CardPanel.AddFieldLabel(bar, "Operation", 18, 10);

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.BaseFont,
                Location = new Point(18, 30),
                Width = 260
            };
            foreach (var action in actions) combo.Items.Add(new ActionItem(action));
            bar.Controls.Add(combo);

            // Selecting an item runs it immediately (rather than requiring a separate "Run"
            // button) - a dropdown reads as "current mode", not a one-time trigger, so its
            // result should always reflect whatever's currently selected.
            combo.SelectedIndexChanged += (_, _) =>
            {
                if (combo.SelectedItem is ActionItem selected) RunAction(selected.Action);
            };

            var defaultIndex = 0;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Primary) defaultIndex = i;
            }
            combo.SelectedIndex = defaultIndex;

            _lblError.Location = new Point(300, 34);
            _lblError.Size = new Size(bar.Width - 318, 26);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);
        }

        private sealed class ActionItem
        {
            public ActionItem(TextTransformAction action) => Action = action;

            public TextTransformAction Action { get; }

            public override string ToString() => Action.Label;
        }

        // A fixed-width, absolutely-positioned single row (the previous approach) silently
        // clips whichever buttons don't fit once a tool has enough actions - they become
        // unclickable with no visual indication anything's missing. A FlowLayoutPanel wraps
        // onto as many rows as needed and both it and the containing bar auto-size to fit,
        // so this scales to however many actions a tool ends up with.
        private void BuildActionBar(IReadOnlyList<TextTransformAction> actions)
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 14)
            };
            Controls.Add(bar);

            // Added first so it ends up below the button flow - same-edge Dock=Top siblings
            // stack with the last-added one closest to the edge (see the docking-order note
            // used throughout this app), so the flow panel (added second, below) renders on top.
            _lblError.Dock = DockStyle.Top;
            _lblError.Height = 26;
            _lblError.Padding = new Padding(18, 4, 18, 0);
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(18, 8, 18, 0)
            };
            bar.Controls.Add(flow);

            Button? defaultActive = null;
            foreach (var action in actions)
            {
                var btn = new Button
                {
                    Text = action.Label,
                    AutoSize = false,
                    Size = new Size(Math.Max(120, action.Label.Length * 8), 32),
                    Margin = new Padding(0, 0, 10, 10)
                };
                Theme.StyleSecondaryButton(btn);
                btn.Click += (_, _) =>
                {
                    SetActiveButton(btn);
                    RunAction(action);
                };
                flow.Controls.Add(btn);

                if (action.Primary) defaultActive = btn;
            }
            // Highlight the designated default button without running it, so the bar shows a
            // starting selection but the output stays blank until the user actually clicks one.
            if (defaultActive is not null) SetActiveButton(defaultActive);
        }

        // Restyles whichever button was previously the "active" one back to secondary and the
        // newly-clicked one to primary, so exactly one button is ever highlighted blue - the one
        // OnInputChanged will keep re-running as you type.
        private void SetActiveButton(Button button)
        {
            if (_activeButton == button) return;
            if (_activeButton is not null) Theme.StyleSecondaryButton(_activeButton);
            Theme.StylePrimaryButton(button);
            _activeButton = button;
        }

        /// <summary>Builds the resizable split view: your input on the left, the transform's result on the right - using CardPanel's shared split-view scaffolding, so it stays in sync with JsonFormatterControl and any other tool built the same way.</summary>
        private void BuildSplitView(Panel card, string inputTitle, string outputTitle, TextTransformContentKind contentKind, TextTransformContentKind outputContentKind)
        {
            var split = CardPanel.AddSplitView(card);

            _input.ScrollBars = RichTextBoxScrollBars.Vertical;
            _input.AcceptsTab = true;
            switch (contentKind)
            {
                case TextTransformContentKind.Json:
                    _input.TextChanged += (_, _) => JsonHighlighter.Highlight(_input);
                    break;
                case TextTransformContentKind.Markup:
                    _input.TextChanged += (_, _) => MarkupHighlighter.Highlight(_input);
                    break;
            }
            CardPanel.FillSplitPane(split.Panel1, inputTitle, _input);

            _output.ReadOnly = true;
            _output.ScrollBars = RichTextBoxScrollBars.Vertical;
            // A converter whose result is itself JSON/XML-shaped (CSV to JSON, XML to JSON, ...)
            // gets the same per-token colorization as a Formatter's output, instead of staying
            // plain black-on-white just because it's produced by the generic TextTransformControl
            // shell rather than a dedicated output view. Left at PlainText (a no-op here) for
            // validators and every plain string-in/string-out tool, which either color the whole
            // result via ApplyResultStyle below or don't need coloring at all.
            switch (outputContentKind)
            {
                case TextTransformContentKind.Json:
                    _output.TextChanged += (_, _) => JsonHighlighter.Highlight(_output);
                    break;
                case TextTransformContentKind.Markup:
                    _output.TextChanged += (_, _) => MarkupHighlighter.Highlight(_output);
                    break;
            }
            CardPanel.FillSplitPane(split.Panel2, outputTitle, _output, onCopy: () =>
            {
                if (_output.Text.Length > 0) Clipboard.SetText(_output.Text);
            });
        }

        private void RunAction(TextTransformAction action)
        {
            _lastAction = action;
            try
            {
                _output.Text = action.Transform(_input.Text);
                HideError();
                ApplyResultStyle();
            }
            catch (Exception ex)
            {
                _output.Text = string.Empty;
                ShowError(ex.Message);
            }
        }

        /// <summary>Colors and bolds the whole result green/red per _isSuccessResult (a validator's pass/fail), so the outcome reads at a glance. A no-op for every other tool, which leaves _isSuccessResult null.</summary>
        private void ApplyResultStyle()
        {
            if (_isSuccessResult is null || _output.TextLength == 0) return;

            var color = _isSuccessResult(_output.Text) ? Theme.Success : Theme.Error;
            _output.SelectAll();
            _output.SelectionColor = color;
            _output.SelectionFont = Theme.BoldFont;
            _output.Select(0, 0);
        }

        private void ShowError(string message)
        {
            _lblError.Text = message;
            _lblError.Visible = true;
        }

        private void HideError() => _lblError.Visible = false;
    }
}
