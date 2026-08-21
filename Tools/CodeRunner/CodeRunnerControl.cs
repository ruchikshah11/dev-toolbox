using System.Diagnostics;
using DevToolbox.UI;

namespace DevToolbox.Tools.CodeRunner
{
    /// <summary>
    /// A live-ish "run this like a terminal would" tool, not a sandbox: your code on the left,
    /// its output on the right - the same side-by-side split-view convention as JSON Formatter /
    /// HTML Viewer, rather than stacking the editor above the output. The language/timeout/Run
    /// controls live in an action bar above both panes. Follows the same CardPanel/Theme
    /// conventions as every other tool; every Label/CheckBox/TextBox below gets an explicit
    /// ForeColor/BackColor so nothing goes invisible under a non-default theme.
    /// </summary>
    public class CodeRunnerControl : UserControl
    {
        private readonly RichTextBox _txtCode = new();
        private readonly RichTextBox _rtbOutput = new();

        private readonly ComboBox _cboLanguage = new();
        private readonly Button _btnRecheck = new();
        private readonly NumericUpDown _numTimeout = new();
        private readonly Button _btnChooseFile = new();
        private readonly Button _btnRun = new();
        private readonly Label _lblStatus = new();
        private readonly Label _lblSafetyNote = new();

        private bool _isRunning;

        public CodeRunnerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top bar below it - see the docking order
            // note in MainForm/JsonFormatterControl.
            var card = CardPanel.Add(this, "Code Runner - your code on the left, output on the right", 0, fill: true);
            BuildSplitView(card);

            BuildActionBar();

            _cboLanguage.SelectedIndexChanged += (_, _) => { UpdateTimeoutEnabledState(); HighlightCode(); };
            _txtCode.TextChanged += (_, _) => HighlightCode();
            RepopulateLanguageItems();
            UpdateTimeoutEnabledState();
            HighlightCode();
        }

        /// <summary>Builds the resizable split view: code editor on the left, run output on the right - using CardPanel's shared split-view scaffolding, the same one JSON Formatter/HTML Viewer use.</summary>
        private void BuildSplitView(Panel card)
        {
            var split = CardPanel.AddSplitView(card);

            // FillSplitPane sets BorderStyle/BackColor/ForeColor for a TextBoxBase automatically,
            // but not Font - set explicitly here so code/output render in the same monospace family
            // as every other code-editing tool in this app. A RichTextBox (not a plain TextBox) so
            // it can be syntax-colored like every other code-editing pane in this app - see
            // HighlightCode()/CodeRunnerHighlighter.
            _txtCode.Font = Theme.MonoFont;
            _txtCode.Multiline = true;
            _txtCode.ScrollBars = RichTextBoxScrollBars.Both;
            _txtCode.WordWrap = false;
            _txtCode.AcceptsTab = true;
            CardPanel.FillSplitPane(split.Panel1, "Code", _txtCode);

            _rtbOutput.Font = Theme.MonoFont;
            _rtbOutput.ReadOnly = true;
            _rtbOutput.WordWrap = false;
            CardPanel.FillSplitPane(split.Panel2, "Output", _rtbOutput, onCopy: () =>
            {
                if (_rtbOutput.TextLength > 0) Clipboard.SetText(_rtbOutput.Text);
            });
        }

        /// <summary>Builds the action bar above the split view: language + timeout + recheck (row 1), choose file (top-right), Run + status (row 2), and the safety note (row 3).</summary>
        private void BuildActionBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 194,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 14)
            };
            Controls.Add(bar);

            CardPanel.AddFieldLabel(bar, "Language", 18, 4);
            _cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboLanguage.Font = Theme.BaseFont;
            _cboLanguage.Location = new Point(18, 24);
            _cboLanguage.Width = 260;
            bar.Controls.Add(_cboLanguage);

            _btnRecheck.Text = "Recheck";
            _btnRecheck.UseMnemonic = false;
            _btnRecheck.Location = new Point(286, 23);
            _btnRecheck.Size = new Size(90, 26);
            Theme.StyleSecondaryButton(_btnRecheck);
            _btnRecheck.Click += OnRecheckClick;
            bar.Controls.Add(_btnRecheck);

            CardPanel.AddFieldLabel(bar, "Timeout (seconds)", 392, 4);
            _numTimeout.Location = new Point(392, 24);
            _numTimeout.Width = 90;
            _numTimeout.Minimum = 1;
            _numTimeout.Maximum = 60;
            _numTimeout.Value = 10;
            _numTimeout.Font = Theme.BaseFont;
            bar.Controls.Add(_numTimeout);

            var lblHint = new Label
            {
                Text = "Script is killed if it runs longer than this.",
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Background,
                Font = Theme.BaseFont,
                AutoSize = true,
                Location = new Point(494, 27)
            };
            bar.Controls.Add(lblHint);

            _btnChooseFile.Text = "Choose File";
            _btnChooseFile.UseMnemonic = false;
            _btnChooseFile.Size = new Size(110, 28);
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += (_, _) => OnChooseFileClick();
            bar.Controls.Add(_btnChooseFile);
            void PositionChooseFile() => _btnChooseFile.Location = new Point(bar.Width - 18 - _btnChooseFile.Width, 22);
            bar.Resize += (_, _) => PositionChooseFile();
            PositionChooseFile();

            _btnRun.Text = "Run";
            _btnRun.UseMnemonic = false;
            _btnRun.Location = new Point(18, 68);
            _btnRun.Size = new Size(140, 34);
            Theme.StylePrimaryButton(_btnRun);
            _btnRun.Click += OnRunClick;
            bar.Controls.Add(_btnRun);

            _lblStatus.AutoSize = false;
            _lblStatus.Font = Theme.BoldFont;
            _lblStatus.BackColor = Theme.Background;
            _lblStatus.TextAlign = ContentAlignment.MiddleRight;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.Size = new Size(440, 24);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _lblStatus.Visible = false;
            bar.Controls.Add(_lblStatus);
            void PositionStatus() => _lblStatus.Location = new Point(bar.Width - 18 - _lblStatus.Width, 78);
            bar.Resize += (_, _) => PositionStatus();
            PositionStatus();

            _lblSafetyNote.Text = "Code runs directly on this machine using your installed language toolchains "
                                  + "(PowerShell, Python, Node.js, cmd.exe, Java, R, GCC/G++) - treat it the same as "
                                  + "running it yourself in a terminal. HTML is opened directly in your default browser instead.";
            _lblSafetyNote.ForeColor = Theme.TextMuted;
            _lblSafetyNote.BackColor = Theme.Background;
            _lblSafetyNote.Font = Theme.BaseFont;
            _lblSafetyNote.Location = new Point(18, 112);
            _lblSafetyNote.Size = new Size(bar.Width - 36, 54);
            _lblSafetyNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            bar.Controls.Add(_lblSafetyNote);
        }

        /// <summary>Populates the language dropdown from CodeRunnerService.Languages, appending " (not found)" to any language whose interpreter/compiler can't actually be started right now (HTML never gets that suffix - it needs no toolchain), and selects the first available language (or index 0 if none are).</summary>
        private void RepopulateLanguageItems()
        {
            var previousIndex = _cboLanguage.SelectedIndex;

            _cboLanguage.Items.Clear();
            foreach (var language in CodeRunnerService.Languages)
            {
                var available = CodeRunnerService.IsAvailable(language);
                _cboLanguage.Items.Add(available ? language.Name : $"{language.Name} (not found)");
            }

            _cboLanguage.SelectedIndex = previousIndex >= 0 && previousIndex < _cboLanguage.Items.Count
                ? previousIndex
                : DefaultLanguageIndex();
        }

        private static int DefaultLanguageIndex()
        {
            for (var i = 0; i < CodeRunnerService.Languages.Length; i++)
            {
                if (CodeRunnerService.IsAvailable(CodeRunnerService.Languages[i])) return i;
            }
            return 0;
        }

        private void OnRecheckClick(object? sender, EventArgs e)
        {
            CodeRunnerService.RecheckAvailability();
            RepopulateLanguageItems();
        }

        /// <summary>Loads a script file into the editor, and auto-selects the matching language if the file's extension is one of the supported ones.</summary>
        private void OnChooseFileClick()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose a script file",
                Filter = "Supported scripts (*.ps1;*.py;*.js;*.bat;*.java;*.html;*.R;*.c;*.cpp)|*.ps1;*.py;*.js;*.bat;*.java;*.html;*.R;*.c;*.cpp|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _txtCode.Text = File.ReadAllText(dialog.FileName);

            var extension = Path.GetExtension(dialog.FileName);
            for (var i = 0; i < CodeRunnerService.Languages.Length; i++)
            {
                if (string.Equals(CodeRunnerService.Languages[i].FileExtension, extension, StringComparison.OrdinalIgnoreCase))
                {
                    _cboLanguage.SelectedIndex = i;
                    break;
                }
            }
        }

        private LanguageDefinition? SelectedLanguage()
        {
            var index = _cboLanguage.SelectedIndex;
            return index >= 0 && index < CodeRunnerService.Languages.Length ? CodeRunnerService.Languages[index] : null;
        }

        /// <summary>HTML has no meaningful timeout (it isn't run as a killable process - see OpenInBrowser), so the control is disabled whenever HTML is the selected language.</summary>
        private void UpdateTimeoutEnabledState()
        {
            var language = SelectedLanguage();
            _numTimeout.Enabled = language is not { Kind: LanguageKind.OpenInBrowser };
        }

        /// <summary>Re-colors the CODE editor for whichever language is currently selected - HTML reuses this app's existing tag-markup tokenizer, the other 7 languages get the generic string/comment/number highlighter. Runs on every keystroke and every language change.</summary>
        private void HighlightCode() => CodeRunnerHighlighter.Highlight(_txtCode, SelectedLanguage());

        /// <summary>
        /// Runs the current code on a background thread (Task.Run) so the up-to-timeoutSeconds
        /// wait never blocks the WinForms message loop, then marshals back to the UI thread via
        /// the continuation after await (the calling SynchronizationContext is captured
        /// automatically) to render the result. Guards against overlapping runs with _isRunning
        /// and a disabled/"Running..." button. HTML branches off into OpenInBrowser instead of
        /// Run() - it isn't a process execution with stdout/stderr to capture, see
        /// CodeRunnerService.OpenInBrowser's own comment for why.
        /// </summary>
        private async void OnRunClick(object? sender, EventArgs e)
        {
            if (_isRunning) return;

            var language = SelectedLanguage();
            if (language is null) return;

            if (language.Kind == LanguageKind.OpenInBrowser)
            {
                RunOpenInBrowser();
                return;
            }

            if (!CodeRunnerService.IsAvailable(language))
            {
                var reason = language.Kind == LanguageKind.Compiled
                    ? $"{language.Name} compiler (gcc/g++) isn't installed or isn't on PATH."
                    : $"{language.Name} isn't installed or isn't on PATH.";
                ShowStatus(reason, isError: true);
                return;
            }

            var code = _txtCode.Text;
            var timeoutSeconds = (int)_numTimeout.Value;

            _isRunning = true;
            _btnRun.Enabled = false;
            _btnRun.Text = "Running...";
            _rtbOutput.Clear();
            HideStatus();

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await Task.Run(() => CodeRunnerService.Run(language, code, timeoutSeconds));
                stopwatch.Stop();
                RenderResult(result, stopwatch.Elapsed);
            }
            finally
            {
                _isRunning = false;
                _btnRun.Enabled = true;
                _btnRun.Text = "Run";
            }
        }

        /// <summary>HTML's "run" is just opening it in the default browser - fast and not something that can hang, so this runs synchronously on the UI thread rather than through the Task.Run/timeout plumbing the other languages use.</summary>
        private void RunOpenInBrowser()
        {
            _rtbOutput.Clear();
            var result = CodeRunnerService.OpenInBrowser(_txtCode.Text);
            AppendColored(result.Message, result.Success ? Theme.Text : Theme.Error);
            ShowStatus(result.Success ? "Opened in your default browser." : "Failed to open in browser.", isError: !result.Success);
        }

        /// <summary>
        /// Renders a RunResult. For a Compiled language (BuildStdout/BuildStderr not null - see
        /// RunResult's own doc comment for why that's the signal), the build's own output is shown
        /// first under a "BUILD OUTPUT" header, clearly separate from the program's own output
        /// under "PROGRAM OUTPUT" - and if the build failed, a distinct "BUILD FAILED" banner is
        /// shown instead of a program output section at all, since the program never ran. This is
        /// deliberately NOT concatenated into one undifferentiated blob - a build failure and a
        /// runtime failure are different problems a user needs to tell apart at a glance.
        /// </summary>
        private void RenderResult(RunResult result, TimeSpan elapsed)
        {
            _rtbOutput.SuspendLayout();
            _rtbOutput.Clear();

            var isCompiled = result.BuildStdout is not null || result.BuildStderr is not null;

            if (isCompiled)
            {
                AppendHeader("BUILD OUTPUT");
                AppendColored(result.BuildStdout ?? string.Empty, Theme.Text);
                if (!string.IsNullOrEmpty(result.BuildStderr))
                {
                    if (_rtbOutput.TextLength > 0) AppendColored(Environment.NewLine, Theme.Text);
                    AppendColored(result.BuildStderr!, Theme.Error);
                }

                if (result.BuildFailed)
                {
                    AppendColored(Environment.NewLine, Theme.Text);
                    AppendColored($"BUILD FAILED (exit code {ExitCodeText(result.BuildExitCode)}) - program was not run.", Theme.Error, bold: true);
                }
                else
                {
                    AppendColored(Environment.NewLine + Environment.NewLine, Theme.Text);
                    AppendHeader("PROGRAM OUTPUT");
                }
            }

            if (!isCompiled || !result.BuildFailed)
            {
                AppendColored(result.Stdout, Theme.Text);

                if (!string.IsNullOrEmpty(result.Stderr))
                {
                    if (_rtbOutput.TextLength > 0) AppendColored(Environment.NewLine, Theme.Text);
                    AppendColored(result.Stderr, Theme.Error);
                }

                if (result.TimedOut)
                {
                    if (_rtbOutput.TextLength > 0) AppendColored(Environment.NewLine, Theme.Text);
                    AppendColored($"Killed after exceeding the {(int)_numTimeout.Value}s timeout.", Theme.Error, bold: true);
                }
            }

            _rtbOutput.SelectionStart = 0;
            _rtbOutput.SelectionLength = 0;
            _rtbOutput.ResumeLayout();

            // SuspendLayout/ResumeLayout defer child-control layout math only - they don't force a
            // repaint on their own. See the identical note in DiffViewerControl.RenderDiff.
            _rtbOutput.Invalidate();
            _rtbOutput.Update();

            string statusText;
            bool isError;
            if (result.BuildFailed)
            {
                statusText = $"Build failed (exit code {ExitCodeText(result.BuildExitCode)})";
                isError = true;
            }
            else if (result.TimedOut)
            {
                statusText = "Timed out";
                isError = true;
            }
            else
            {
                statusText = $"Exit code: {result.ExitCode}";
                isError = result.ExitCode.HasValue && result.ExitCode.Value != 0;
            }
            ShowStatus($"{statusText}   |   Elapsed: {elapsed.TotalSeconds:0.00}s", isError);
        }

        private static string ExitCodeText(int? exitCode) => exitCode?.ToString() ?? "timed out";

        /// <summary>A bold, muted section header ("BUILD OUTPUT" / "PROGRAM OUTPUT") on its own line, in the same monospace family as the rest of the output rather than switching to the app's proportional bold font.</summary>
        private void AppendHeader(string text)
        {
            var start = _rtbOutput.TextLength;
            _rtbOutput.AppendText(text + Environment.NewLine);
            _rtbOutput.Select(start, text.Length);
            _rtbOutput.SelectionFont = new Font(Theme.MonoFont, FontStyle.Bold);
            _rtbOutput.SelectionColor = Theme.TextMuted;
        }

        /// <summary>Appends text to the output box in the given color, without a trailing newline (callers add their own separators).</summary>
        private void AppendColored(string text, Color color, bool bold = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            var start = _rtbOutput.TextLength;
            _rtbOutput.AppendText(text);
            _rtbOutput.Select(start, text.Length);
            _rtbOutput.SelectionColor = color;
            if (bold) _rtbOutput.SelectionFont = new Font(Theme.MonoFont, FontStyle.Bold);
        }

        private void ShowStatus(string message, bool isError)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = isError ? Theme.Error : Theme.TextMuted;
            _lblStatus.Visible = true;
        }

        private void HideStatus() => _lblStatus.Visible = false;
    }
}
