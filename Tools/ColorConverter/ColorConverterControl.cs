using DevToolbox.UI;

namespace DevToolbox.Tools.ColorConverter
{
    public class ColorConverterControl : UserControl
    {
        private readonly TextBox _txtInput = new();
        private readonly Label _lblError = new();
        private readonly Panel _swatch = new();
        private readonly TextBox _txtHex = new();
        private readonly TextBox _txtRgb = new();
        private readonly TextBox _txtHsl = new();

        public ColorConverterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking order
            // note in TextTransformControl/MainForm.
            var resultCard = CardPanel.Add(this, "Result", 0, fill: true);
            BuildResultCard(resultCard);

            BuildInputCard();

            Convert();
        }

        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "Color (hex #RRGGBB, rgb(r, g, b), or hsl(h, s%, l%))", 110);

            _txtInput.Font = Theme.MonoFont;
            _txtInput.Location = new Point(18, 44);
            _txtInput.Width = 340;
            _txtInput.Text = "#2F6FED";
            _txtInput.TextChanged += (_, _) => Convert();
            card.Controls.Add(_txtInput);

            _lblError.Location = new Point(18, 78);
            _lblError.Size = new Size(card.Width - 36, 24);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        private void BuildResultCard(Panel card)
        {
            _swatch.Location = new Point(18, 50);
            _swatch.Size = new Size(140, 140);
            _swatch.BorderStyle = BorderStyle.FixedSingle;
            card.Controls.Add(_swatch);

            AddValueRow(card, "HEX", _txtHex, 50);
            AddValueRow(card, "RGB", _txtRgb, 96);
            AddValueRow(card, "HSL", _txtHsl, 142);
        }

        /// <summary>Adds one labeled, read-only result row (HEX/RGB/HSL) with its own Copy button at the given vertical position.</summary>
        private void AddValueRow(Panel card, string label, TextBox output, int y)
        {
            CardPanel.AddFieldLabel(card, label, 176, y);

            output.ReadOnly = true;
            output.Font = Theme.MonoFont;
            output.Location = new Point(176, y + 20);
            output.Width = 260;
            card.Controls.Add(output);

            // A couple of px taller than the textbox's own (auto-computed) height, not an exact
            // match - flush with it clips the descender off letters like "y", e.g. "Copy"
            // rendering as "Conv".
            var btnHeight = output.Height + 4;
            var btnCopy = new Button { Text = "Copy", Size = new Size(70, btnHeight), Location = new Point(444, y + 20 - (btnHeight - output.Height) / 2) };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (_, _) =>
            {
                if (output.Text.Length > 0) Clipboard.SetText(output.Text);
            };
            card.Controls.Add(btnCopy);
        }

        private void Convert()
        {
            try
            {
                var result = ColorConverterService.Parse(_txtInput.Text);
                _swatch.BackColor = Color.FromArgb(result.R, result.G, result.B);
                _txtHex.Text = result.Hex;
                _txtRgb.Text = result.Rgb;
                _txtHsl.Text = result.Hsl;
                HideError();
            }
            catch (FormatException ex)
            {
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
