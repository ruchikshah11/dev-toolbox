using System.Drawing;
using DevToolbox.UI;

namespace DevToolbox.Tools.ImagePreviewer
{
    public class ImagePreviewerControl : UserControl
    {
        private readonly TextBox _txtInput = new();
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly Label _lblError = new();
        private readonly PictureBox _picturePreview = new();
        private readonly Label _lblInfo = new();
        private readonly Button _btnCopyDataUri = new();
        private readonly Button _btnCopyBase64Only = new();

        private Bitmap? _currentBitmap;
        private string? _lastBase64Only;

        /// <summary>Builds the paste/upload input card and the live image preview card.</summary>
        public ImagePreviewerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking order
            // note used throughout the tool controls.
            BuildPreviewCard();
            BuildInputCard();

            Decode();
        }

        /// <summary>Builds the data-URI/base64 paste box plus its "choose an image file" row and error label.</summary>
        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "DATA URI / BASE64 (paste, or choose an image file below)", 240);

            _txtInput.Multiline = true;
            _txtInput.ScrollBars = ScrollBars.Vertical;
            _txtInput.AcceptsReturn = true;
            _txtInput.AcceptsTab = true;
            _txtInput.TextChanged += (_, _) => Decode();
            // Reserves room below the paste box for BOTH the error label and the button row (a
            // prior version only reserved space for the button row, so the error label - carved
            // out of that same 40px strip via a negative offset - visually overlapped the paste
            // box's own bottom border instead of sitting in a clear gap beneath it).
            CardPanel.WrapWithBorder(card, _txtInput, new Point(18, 42), card.Width - 36, card.Height - 118,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);

            _btnChooseFile.Text = "Choose Image File";
            _btnChooseFile.Size = new Size(150, 30);
            _btnChooseFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += OnChooseFileClick;
            card.Controls.Add(_btnChooseFile);

            _lblFileName.Text = "No file chosen";
            _lblFileName.ForeColor = Theme.TextMuted;
            _lblFileName.Font = Theme.BaseFont;
            // Label defaults to AutoSize=true, which silently ignores whatever Size/Anchor is
            // assigned below and just grows to fit the full unwrapped text instead - the cause
            // of this text overflowing past the card's edge instead of ellipsis-truncating.
            _lblFileName.AutoSize = false;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblFileName);

            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoSize = false;
            _lblError.AutoEllipsis = true;
            _lblError.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);

            void PositionBottomRow()
            {
                var rowTop = card.Height - 38;
                _btnChooseFile.Location = new Point(18, rowTop);
                _lblFileName.Location = new Point(180, rowTop + 5);
                _lblFileName.Size = new Size(Math.Max(40, card.Width - 36 - 180), 22);
                _lblError.Location = new Point(18, rowTop - 30);
                _lblError.Size = new Size(card.Width - 36, 24);
            }
            card.Resize += (_, _) => PositionBottomRow();
            PositionBottomRow();
        }

        /// <summary>Builds the live image preview card, its info line, and the two copy buttons.</summary>
        private void BuildPreviewCard()
        {
            var card = CardPanel.Add(this, "PREVIEW", 0, fill: true);

            _btnCopyDataUri.Text = "Copy Data URI";
            _btnCopyDataUri.Size = new Size(130, 28);
            Theme.StyleSecondaryButton(_btnCopyDataUri);
            _btnCopyDataUri.Click += (_, _) =>
            {
                if (_txtInput.Text.Length > 0) Clipboard.SetText(_txtInput.Text);
            };
            card.Controls.Add(_btnCopyDataUri);

            _btnCopyBase64Only.Text = "Copy Base64 Only";
            _btnCopyBase64Only.Size = new Size(140, 28);
            Theme.StyleSecondaryButton(_btnCopyBase64Only);
            _btnCopyBase64Only.Click += (_, _) =>
            {
                if (_lastBase64Only is { Length: > 0 } base64) Clipboard.SetText(base64);
            };
            card.Controls.Add(_btnCopyBase64Only);

            void PositionCopyButtons()
            {
                _btnCopyDataUri.Location = new Point(card.Width - 18 - _btnCopyDataUri.Width, 8);
                _btnCopyBase64Only.Location = new Point(_btnCopyDataUri.Left - 10 - _btnCopyBase64Only.Width, 8);
            }
            card.Resize += (_, _) => PositionCopyButtons();
            PositionCopyButtons();

            _lblInfo.Location = new Point(18, 42);
            _lblInfo.Size = new Size(card.Width - 36, 22);
            _lblInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblInfo.ForeColor = Theme.TextMuted;
            _lblInfo.Font = Theme.BaseFont;
            _lblInfo.AutoEllipsis = true;
            card.Controls.Add(_lblInfo);

            _picturePreview.Location = new Point(18, 70);
            _picturePreview.Size = new Size(card.Width - 36, card.Height - 88);
            _picturePreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _picturePreview.SizeMode = PictureBoxSizeMode.Zoom;
            _picturePreview.BackColor = Theme.Card;
            _picturePreview.BorderStyle = BorderStyle.FixedSingle;
            card.Controls.Add(_picturePreview);
        }

        /// <summary>Opens a file picker and loads the chosen image's data URI into the paste box.</summary>
        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose an image file",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.ico)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.ico|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _lblFileName.Text = Path.GetFileName(dialog.FileName);
            try
            {
                _txtInput.Text = ImagePreviewerService.EncodeFileToDataUri(dialog.FileName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowError($"Could not read file: {ex.Message}");
            }
        }

        /// <summary>Decodes the current paste-box contents and updates the preview, or shows the error - empty input is just "nothing pasted yet", not a validation failure, so it clears the preview silently instead of showing an error.</summary>
        private void Decode()
        {
            if (string.IsNullOrWhiteSpace(_txtInput.Text))
            {
                _picturePreview.Image = null;
                _currentBitmap?.Dispose();
                _currentBitmap = null;
                _lastBase64Only = null;
                _lblInfo.Text = string.Empty;
                HideError();
                return;
            }

            try
            {
                var result = ImagePreviewerService.Decode(_txtInput.Text);

                _picturePreview.Image = null;
                _currentBitmap?.Dispose();
                _currentBitmap = result.Image;
                _picturePreview.Image = _currentBitmap;
                _lastBase64Only = result.Base64Only;

                _lblInfo.Text = $"{result.MimeType} - {result.WidthPx}x{result.HeightPx}px - {result.ByteCount:N0} bytes";
                HideError();
            }
            catch (Exception ex)
            {
                _picturePreview.Image = null;
                _currentBitmap?.Dispose();
                _currentBitmap = null;
                _lastBase64Only = null;
                _lblInfo.Text = string.Empty;
                ShowError(ex.Message);
            }
        }

        /// <summary>Shows the error label with the given message.</summary>
        private void ShowError(string message)
        {
            _lblError.Text = message;
            _lblError.Visible = true;
        }

        /// <summary>Hides the error label.</summary>
        private void HideError() => _lblError.Visible = false;

        /// <summary>Releases the currently decoded Bitmap along with the control's own resources.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) _currentBitmap?.Dispose();
            base.Dispose(disposing);
        }
    }
}
