using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.PasswordGenerator
{
    public class PasswordGeneratorControl : UserControl
    {
        private static readonly string[] Separators = { "-", ".", "_", "?", "!", "*" };

        private readonly ComboBox _cboType = new();
        private readonly Panel _pnlPassword = new();
        private readonly Panel _pnlPassphrase = new();

        private readonly TrackBar _trkLength = new();
        private readonly Label _lblLengthValue = new();
        private readonly CheckBox _chkLowercase = new();
        private readonly CheckBox _chkUppercase = new();
        private readonly CheckBox _chkDigits = new();
        private readonly CheckBox _chkSymbols = new();

        private readonly TrackBar _trkWords = new();
        private readonly Label _lblWordsValue = new();
        private readonly CheckBox _chkCapitalize = new();
        private readonly CheckBox _chkNumber = new();
        private readonly Button[] _btnSeparators = new Button[Separators.Length];
        private string _selectedSeparator = "-";

        private readonly Button _btnGenerate = new();
        private readonly Button _btnHistory = new();
        private readonly Label _lblError = new();

        private readonly TextBox _txtOutput = new();
        private readonly Label _lblStrength = new();
        private readonly Panel _pnlStrengthBar = new();
        private double _strengthFraction;

        /// <summary>Builds the tool's two cards (options on top, generated output filling the rest) and generates an initial value.</summary>
        public PasswordGeneratorControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking
            // order note in MainForm/JsonFormatterControl.
            BuildOutputCard();
            BuildOptionsCard();

            Generate();
        }

        /// <summary>Builds the OPTIONS card: the Type dropdown, both the Password and Passphrase option panels (only one visible at a time), and the Generate button.</summary>
        private void BuildOptionsCard()
        {
            var card = CardPanel.Add(this, "OPTIONS", 380);

            CardPanel.AddFieldLabel(card, "Type", 18, 44);
            _cboType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboType.Font = Theme.BaseFont;
            _cboType.Location = new Point(18, 64);
            _cboType.Width = 200;
            _cboType.Items.AddRange(new object[] { "Password", "Passphrase" });
            _cboType.SelectedIndex = 0;
            _cboType.SelectedIndexChanged += (_, _) => { SwitchType(); Generate(); };
            card.Controls.Add(_cboType);

            BuildPasswordPanel();
            BuildPassphrasePanel();
            card.Controls.Add(_pnlPassword);
            card.Controls.Add(_pnlPassphrase);
            SwitchType();

            _btnGenerate.Text = "Generate";
            _btnGenerate.Location = new Point(18, 318);
            _btnGenerate.Size = new Size(140, 32);
            Theme.StylePrimaryButton(_btnGenerate);
            _btnGenerate.Click += (_, _) => { Generate(); RecordHistory(); };
            card.Controls.Add(_btnGenerate);

            _btnHistory.Text = "History";
            _btnHistory.Location = new Point(166, 318);
            _btnHistory.Size = new Size(100, 32);
            Theme.StyleSecondaryButton(_btnHistory);
            _btnHistory.Click += (_, _) => new PasswordHistoryForm().Show();
            card.Controls.Add(_btnHistory);

            _lblError.Location = new Point(274, 322);
            _lblError.Size = new Size(card.Width - 36 - 256, 26);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        /// <summary>Builds the Password-mode options sub-panel: length slider and the four character-type checkboxes.</summary>
        private void BuildPasswordPanel()
        {
            _pnlPassword.Location = new Point(18, 110);
            _pnlPassword.Size = new Size(760, 190);
            _pnlPassword.BackColor = Theme.Card;

            CardPanel.AddFieldLabel(_pnlPassword, "Length", 0, 0);
            _lblLengthValue.Font = Theme.BaseFont;
            _lblLengthValue.ForeColor = Theme.TextMuted;
            _lblLengthValue.AutoSize = true;
            _lblLengthValue.Location = new Point(260, 0);
            _pnlPassword.Controls.Add(_lblLengthValue);

            _trkLength.Location = new Point(0, 20);
            _trkLength.Width = 400;
            _trkLength.Minimum = 4;
            _trkLength.Maximum = AppSettings.Load().PasswordMaxLength;
            _trkLength.Value = Math.Min(20, _trkLength.Maximum);
            _trkLength.TickFrequency = 5;
            _trkLength.ValueChanged += (_, _) => { UpdateLengthLabel(); Generate(); };
            _pnlPassword.Controls.Add(_trkLength);
            UpdateLengthLabel();

            _chkLowercase.Text = "Lowercase";
            _chkLowercase.Checked = true;
            _chkUppercase.Text = "Uppercase";
            _chkUppercase.Checked = true;
            _chkDigits.Text = "Numbers";
            _chkDigits.Checked = true;
            _chkSymbols.Text = "Symbols";
            _chkSymbols.Checked = true;

            var checkboxes = new[] { _chkLowercase, _chkUppercase, _chkDigits, _chkSymbols };
            for (var i = 0; i < checkboxes.Length; i++)
            {
                var chk = checkboxes[i];
                chk.Font = Theme.BaseFont;
                chk.ForeColor = Theme.Text;
                chk.AutoSize = true;
                chk.Location = new Point(i * 140, 70);
                chk.CheckedChanged += (_, _) => Generate();
                _pnlPassword.Controls.Add(chk);
            }
        }

        /// <summary>Builds the Passphrase-mode options sub-panel: word count slider, Capitals/Number checkboxes, and the separator button row.</summary>
        private void BuildPassphrasePanel()
        {
            _pnlPassphrase.Location = new Point(18, 110);
            _pnlPassphrase.Size = new Size(760, 190);
            _pnlPassphrase.BackColor = Theme.Card;

            CardPanel.AddFieldLabel(_pnlPassphrase, "Words", 0, 0);
            _lblWordsValue.Font = Theme.BaseFont;
            _lblWordsValue.ForeColor = Theme.TextMuted;
            _lblWordsValue.AutoSize = true;
            _lblWordsValue.Location = new Point(260, 0);
            _pnlPassphrase.Controls.Add(_lblWordsValue);

            _trkWords.Location = new Point(0, 20);
            _trkWords.Width = 400;
            _trkWords.Minimum = 2;
            _trkWords.Maximum = 12;
            _trkWords.Value = 5;
            _trkWords.TickFrequency = 1;
            _trkWords.ValueChanged += (_, _) => { UpdateWordsLabel(); Generate(); };
            _pnlPassphrase.Controls.Add(_trkWords);
            UpdateWordsLabel();

            _chkCapitalize.Text = "Capitals";
            _chkCapitalize.Checked = true;
            _chkCapitalize.Font = Theme.BaseFont;
            _chkCapitalize.ForeColor = Theme.Text;
            _chkCapitalize.AutoSize = true;
            _chkCapitalize.Location = new Point(0, 70);
            _chkCapitalize.CheckedChanged += (_, _) => Generate();
            _pnlPassphrase.Controls.Add(_chkCapitalize);

            _chkNumber.Text = "Number";
            _chkNumber.Checked = true;
            _chkNumber.Font = Theme.BaseFont;
            _chkNumber.ForeColor = Theme.Text;
            _chkNumber.AutoSize = true;
            _chkNumber.Location = new Point(140, 70);
            _chkNumber.CheckedChanged += (_, _) => Generate();
            _pnlPassphrase.Controls.Add(_chkNumber);

            CardPanel.AddFieldLabel(_pnlPassphrase, "Separator", 0, 116);
            for (var i = 0; i < Separators.Length; i++)
            {
                var separator = Separators[i];
                var btn = new Button
                {
                    Text = separator,
                    Size = new Size(36, 30),
                    Location = new Point(i * 42, 138),
                    FlatStyle = FlatStyle.Flat,
                    Font = Theme.BoldFont,
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 1;
                // Flat buttons auto-shade a default grey overlay on hover/press unless these are
                // set explicitly - without it, unselected buttons flash a mismatched grey box
                // (as seen on hover) instead of the theme's own hover tint.
                btn.FlatAppearance.MouseOverBackColor = Theme.AccentSoft;
                btn.FlatAppearance.MouseDownBackColor = Theme.AccentSoft;
                btn.Click += (_, _) => { _selectedSeparator = separator; RefreshSeparatorButtons(); Generate(); };
                _btnSeparators[i] = btn;
                _pnlPassphrase.Controls.Add(btn);
            }
            RefreshSeparatorButtons();
        }

        /// <summary>Repaints all separator buttons so the currently selected one is highlighted and the rest are not.</summary>
        private void RefreshSeparatorButtons()
        {
            for (var i = 0; i < Separators.Length; i++)
            {
                var selected = Separators[i] == _selectedSeparator;
                var btn = _btnSeparators[i];
                btn.BackColor = selected ? Theme.Accent : Theme.Card;
                btn.ForeColor = selected ? Color.White : Theme.Text;
                btn.FlatAppearance.BorderColor = selected ? Theme.Accent : Theme.Border;
            }
        }

        /// <summary>Shows the Password or Passphrase options sub-panel based on the Type dropdown's current selection.</summary>
        private void SwitchType()
        {
            var isPassword = _cboType.SelectedIndex == 0;
            _pnlPassword.Visible = isPassword;
            _pnlPassphrase.Visible = !isPassword;
        }

        /// <summary>Refreshes the "N Characters" label next to the length slider.</summary>
        private void UpdateLengthLabel() => _lblLengthValue.Text = $"{_trkLength.Value} Characters";

        /// <summary>Refreshes the "N Words" label next to the word count slider.</summary>
        private void UpdateWordsLabel() => _lblWordsValue.Text = $"{_trkWords.Value} Words";

        /// <summary>Builds the GENERATED card: the output textbox, its Copy button, and the strength label/bar underneath it.</summary>
        private void BuildOutputCard()
        {
            var card = CardPanel.Add(this, "GENERATED", 0, fill: true);

            var btnCopy = new Button { Text = "Copy to Clipboard", Size = new Size(150, 28) };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (_, _) =>
            {
                if (_txtOutput.Text.Length == 0) return;
                Clipboard.SetText(_txtOutput.Text);
                ClipboardAutoClear.ScheduleClear(_txtOutput.Text);
            };
            card.Controls.Add(btnCopy);

            void PositionCopy() => btnCopy.Location = new Point(card.Width - 18 - btnCopy.Width, 8);
            card.Resize += (_, _) => PositionCopy();
            PositionCopy();

            _txtOutput.Multiline = true;
            _txtOutput.ReadOnly = true;
            _txtOutput.WordWrap = true;
            _txtOutput.ScrollBars = ScrollBars.Vertical;
            _txtOutput.Font = Theme.MonoFont;
            CardPanel.WrapWithBorder(card, _txtOutput, new Point(18, 42), card.Width - 36, card.Height - 92,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);

            _lblStrength.Font = Theme.BoldFont;
            _lblStrength.AutoSize = true;
            _lblStrength.Location = new Point(18, card.Height - 40);
            _lblStrength.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            card.Controls.Add(_lblStrength);

            _pnlStrengthBar.Location = new Point(120, card.Height - 36);
            _pnlStrengthBar.Size = new Size(card.Width - 36 - 102, 8);
            _pnlStrengthBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _pnlStrengthBar.Paint += (_, e) => PaintStrengthBar(e.Graphics);
            card.Controls.Add(_pnlStrengthBar);
        }

        /// <summary>Draws the strength bar's track and its proportional colored fill.</summary>
        private void PaintStrengthBar(Graphics g)
        {
            using var trackBrush = new SolidBrush(Theme.Border);
            g.FillRectangle(trackBrush, 0, 0, _pnlStrengthBar.Width, _pnlStrengthBar.Height);

            var fillWidth = (int)(_pnlStrengthBar.Width * _strengthFraction);
            if (fillWidth <= 0) return;

            using var fillBrush = new SolidBrush(_lblStrength.ForeColor);
            g.FillRectangle(fillBrush, 0, 0, fillWidth, _pnlStrengthBar.Height);
        }

        /// <summary>Generates a new password or passphrase (per the current Type/options) into the output box and updates the strength meter, or shows a validation error.</summary>
        private void Generate()
        {
            try
            {
                double entropyBits;
                if (_cboType.SelectedIndex == 0)
                {
                    _txtOutput.Text = PasswordGeneratorService.GeneratePassword(
                        _trkLength.Value, _chkLowercase.Checked, _chkUppercase.Checked, _chkDigits.Checked, _chkSymbols.Checked);
                    entropyBits = PasswordGeneratorService.PasswordEntropyBits(
                        _trkLength.Value, _chkLowercase.Checked, _chkUppercase.Checked, _chkDigits.Checked, _chkSymbols.Checked);
                }
                else
                {
                    _txtOutput.Text = PasswordGeneratorService.GeneratePassphrase(
                        _trkWords.Value, _chkCapitalize.Checked, _chkNumber.Checked, _selectedSeparator);
                    entropyBits = PasswordGeneratorService.PassphraseEntropyBits(_trkWords.Value, _chkNumber.Checked);
                }

                UpdateStrength(entropyBits);
                _lblError.Visible = false;
            }
            catch (FormatException ex)
            {
                _txtOutput.Text = string.Empty;
                UpdateStrength(0);
                _lblError.Text = ex.Message;
                _lblError.Visible = true;
            }
        }

        /// <summary>Saves the current output to the (DPAPI-encrypted) history - called only from an explicit Generate click, not on every live options change, so dragging a slider doesn't flood history with intermediate values.</summary>
        private void RecordHistory()
        {
            if (_txtOutput.Text.Length == 0) return;
            var type = _cboType.SelectedIndex == 0 ? "Password" : "Passphrase";
            PasswordHistoryStore.Add(_txtOutput.Text, type);
        }

        /// <summary>Classifies the given entropy value and updates the strength label's text/color and the bar's fill fraction.</summary>
        private void UpdateStrength(double entropyBits)
        {
            var strength = PasswordGeneratorService.Classify(entropyBits);
            _lblStrength.Text = strength switch
            {
                PasswordStrength.Strong => "Strong",
                PasswordStrength.Fair => "Fair",
                _ => "Weak"
            };
            _lblStrength.ForeColor = strength switch
            {
                PasswordStrength.Strong => Theme.Success,
                PasswordStrength.Fair => Theme.Warning,
                _ => Theme.Error
            };

            // Entropy has no hard ceiling, so scale against a generous 100-bit reference point
            // purely to size the bar - it's a relative indicator, not a calibrated percentage.
            _strengthFraction = Math.Min(1.0, entropyBits / 100.0);
            _pnlStrengthBar.Invalidate();
        }
    }
}
