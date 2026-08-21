using DevToolbox.UI;

namespace DevToolbox.Tools.CronGenerator
{
    public class CronGeneratorControl : UserControl
    {
        private readonly TextBox _txtExpression = new();
        private readonly Button _btnCompute = new();
        private readonly Label _lblError = new();
        private readonly TextBox _txtOutput = new();

        public CronGeneratorControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking
            // order note in MainForm/JsonFormatterControl.
            var outputCard = CardPanel.Add(this, "Next 5 Fire Times", 0, fill: true);
            BuildOutputCard(outputCard);

            BuildExpressionCard();
        }

        private void BuildExpressionCard()
        {
            var card = CardPanel.Add(this, "Cron Expression (Quartz, 6 fields)", 240);

            CardPanel.AddFieldLabel(card, "seconds minutes hours day-of-month month day-of-week", 18, 42);
            _txtExpression.Font = Theme.MonoFont;
            _txtExpression.Location = new Point(18, 62);
            _txtExpression.Width = card.Width - 36;
            _txtExpression.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtExpression.Text = "0 0/5 * * * ?";
            card.Controls.Add(_txtExpression);

            var lblExample = new Label
            {
                Text = "Example: \"0 0/5 * * * ?\" fires every 5 minutes. Supports *, ?, single values, ranges " +
                       "(a-b), steps (*/n or a-b/n) and comma lists.",
                ForeColor = Theme.TextMuted,
                Font = Theme.BaseFont,
                Location = new Point(18, 92),
                Size = new Size(card.Width - 36, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };
            card.Controls.Add(lblExample);

            _btnCompute.Text = "Compute Next Fire Times";
            _btnCompute.Location = new Point(18, 134);
            _btnCompute.Size = new Size(200, 32);
            Theme.StylePrimaryButton(_btnCompute);
            _btnCompute.Click += (_, _) => Compute();
            card.Controls.Add(_btnCompute);

            _lblError.Location = new Point(226, 138);
            _lblError.Size = new Size(card.Width - 36 - 208, 26);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);

            var lblNote = new Label
            {
                Text = "Note: Quartz extensions L, W and # (e.g. \"L\" for last day, \"6#3\" for the 3rd Friday) " +
                       "are NOT supported - \"?\" is treated the same as \"*\" for computation purposes.",
                ForeColor = Theme.TextMuted,
                Font = Theme.BaseFont,
                Location = new Point(18, 172),
                Size = new Size(card.Width - 36, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };
            card.Controls.Add(lblNote);
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

        private void Compute()
        {
            try
            {
                var cron = CronService.Parse(_txtExpression.Text);
                var fireTimes = CronService.GetNextFireTimes(cron, DateTime.Now, 5);
                _txtOutput.Text = string.Join(Environment.NewLine,
                    fireTimes.Select((dt, i) => $"{i + 1}. {dt:yyyy-MM-dd HH:mm:ss} ({dt.DayOfWeek})"));
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
