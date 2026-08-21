using DevToolbox.UI;

namespace DevToolbox.Tools.QrCodeGenerator
{
    public class QrCodeGeneratorControl : UserControl
    {
        private readonly TextBox _txtInput = new();
        private readonly Button _btnGenerate = new();
        private readonly Label _lblError = new();
        private readonly PictureBox _pictureBox = new();
        private readonly Button _btnSaveImage = new();

        private byte[]? _lastPngBytes;

        public QrCodeGeneratorControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top controls below it. Same-edge Dock=Top
            // siblings stack in reverse add-order, so the input card (added last) ends up
            // visually on top, above the action bar, above the QR result area.
            BuildResultCard();
            BuildActionBar();
            BuildInputCard();
        }

        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "TEXT / URL TO ENCODE", 180);
            _txtInput.Multiline = true;
            _txtInput.ScrollBars = ScrollBars.Vertical;
            _txtInput.AcceptsReturn = true;
            _txtInput.AcceptsTab = true;
            CardPanel.WrapWithBorder(card, _txtInput, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void BuildActionBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 14)
            };
            Controls.Add(bar);

            _btnGenerate.Text = "Generate QR Code";
            _btnGenerate.UseMnemonic = false;
            _btnGenerate.Location = new Point(18, 8);
            _btnGenerate.Size = new Size(170, 32);
            Theme.StylePrimaryButton(_btnGenerate);
            _btnGenerate.Click += (_, _) => TryGenerate();
            bar.Controls.Add(_btnGenerate);

            _lblError.Location = new Point(18, 46);
            _lblError.Size = new Size(700, 26);
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            bar.Controls.Add(_lblError);
        }

        private void BuildResultCard()
        {
            var card = CardPanel.Add(this, "QR CODE", 0, fill: true);

            _btnSaveImage.Text = "Save Image...";
            _btnSaveImage.UseMnemonic = false;
            _btnSaveImage.Size = new Size(140, 28);
            _btnSaveImage.Enabled = false;
            Theme.StyleSecondaryButton(_btnSaveImage);
            _btnSaveImage.Click += OnSaveImageClick;
            card.Controls.Add(_btnSaveImage);

            void PositionSaveButton() => _btnSaveImage.Location = new Point(card.Width - 18 - _btnSaveImage.Width, 8);
            card.Resize += (_, _) => PositionSaveButton();
            PositionSaveButton();

            _pictureBox.Location = new Point(18, 50);
            _pictureBox.Size = new Size(260, 260);
            _pictureBox.BorderStyle = BorderStyle.FixedSingle;
            _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _pictureBox.BackColor = Color.White;
            card.Controls.Add(_pictureBox);
        }

        private void TryGenerate()
        {
            try
            {
                var bytes = QrCodeGeneratorService.GeneratePng(_txtInput.Text);
                _lastPngBytes = bytes;

                var previousImage = _pictureBox.Image;
                _pictureBox.Image = Image.FromStream(new MemoryStream(bytes));
                previousImage?.Dispose();

                _btnSaveImage.Enabled = true;
                HideError();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void OnSaveImageClick(object? sender, EventArgs e)
        {
            if (_lastPngBytes is null) return;

            using var dialog = new SaveFileDialog
            {
                Title = "Save QR code image",
                Filter = "PNG image (*.png)|*.png",
                FileName = "qrcode.png"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                File.WriteAllBytes(dialog.FileName, _lastPngBytes);
                HideError();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowError($"Could not save file: {ex.Message}");
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
