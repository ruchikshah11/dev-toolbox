using System.Text.RegularExpressions;
using DevToolbox.UI;

namespace DevToolbox.Tools.JavaRegexTester
{
    public class JavaRegexTesterControl : UserControl
    {
        private readonly TextBox _txtPattern = new();
        private readonly CheckBox _chkIgnoreCase = new();
        private readonly CheckBox _chkMultiline = new();
        private readonly CheckBox _chkSingleline = new();
        private readonly CheckBox _chkIgnoreWhitespace = new();
        private readonly TextBox _txtInput = new();
        private readonly Button _btnTest = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();

        public JavaRegexTesterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it - see the docking
            // order note in MainForm/JsonFormatterControl.
            var outputCard = CardPanel.Add(this, "Results", 0, fill: true);
            BuildOutputCard(outputCard);

            BuildInputCard();
            BuildPatternCard();
        }

        private void BuildPatternCard()
        {
            var card = CardPanel.Add(this, "Pattern", 225);

            var lblNote = new Label
            {
                Text = "Uses .NET's regex engine - functionally very close to java.util.regex for most " +
                       "patterns, but Java-specific features like possessive quantifiers (e.g. \"a++\") are " +
                       "not supported.",
                ForeColor = Theme.TextMuted,
                Font = Theme.BaseFont,
                Location = new Point(18, 42),
                Size = new Size(card.Width - 36, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };
            card.Controls.Add(lblNote);

            _txtPattern.Font = Theme.MonoFont;
            _txtPattern.Location = new Point(18, 78);
            _txtPattern.Width = card.Width - 36;
            _txtPattern.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_txtPattern);

            AddCheck(card, _chkIgnoreCase, "Ignore case", 18, 114);
            AddCheck(card, _chkMultiline, "Multiline", 150, 114);
            AddCheck(card, _chkSingleline, "Singleline", 280, 114);
            AddCheck(card, _chkIgnoreWhitespace, "Ignore pattern whitespace", 410, 114);

            _btnTest.Text = "Test";
            _btnTest.Location = new Point(18, 148);
            _btnTest.Size = new Size(110, 30);
            Theme.StylePrimaryButton(_btnTest);
            _btnTest.Click += (_, _) => RunTest();
            card.Controls.Add(_btnTest);

            _lblError.Location = new Point(140, 152);
            _lblError.Size = new Size(card.Width - 36 - 122, 26);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        private static void AddCheck(Control parent, CheckBox chk, string text, int x, int y)
        {
            chk.Text = text;
            chk.Location = new Point(x, y);
            chk.AutoSize = true;
            chk.Font = Theme.BaseFont;
            chk.ForeColor = Theme.Text;
            chk.UseMnemonic = false;
            parent.Controls.Add(chk);
        }

        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "Test Input", 200);
            _txtInput.Multiline = true;
            _txtInput.ScrollBars = ScrollBars.Vertical;
            _txtInput.AcceptsReturn = true;
            _txtInput.AcceptsTab = true;
            CardPanel.WrapWithBorder(card, _txtInput, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void BuildOutputCard(Panel card)
        {
            _txtOutput.Multiline = true;
            _txtOutput.ReadOnly = true;
            _txtOutput.ScrollBars = ScrollBars.Vertical;
            _txtOutput.Font = Theme.MonoFont;
            CardPanel.WrapWithBorder(card, _txtOutput, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private RegexOptions BuildOptions()
        {
            var options = RegexOptions.None;
            if (_chkIgnoreCase.Checked) options |= RegexOptions.IgnoreCase;
            if (_chkMultiline.Checked) options |= RegexOptions.Multiline;
            if (_chkSingleline.Checked) options |= RegexOptions.Singleline;
            if (_chkIgnoreWhitespace.Checked) options |= RegexOptions.IgnorePatternWhitespace;
            return options;
        }

        private void RunTest()
        {
            try
            {
                _txtOutput.Text = JavaRegexTesterService.Test(_txtPattern.Text, _txtInput.Text, BuildOptions());
                HideError();
            }
            catch (Exception ex)
            {
                _txtOutput.Text = string.Empty;
                ShowError(ex.Message);
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
