using DevToolbox.UI;

namespace DevToolbox.Tools.ColorPicker
{
    /// <summary>
    /// Visual Color Picker: a saturation/value gradient square plus a hue slider, with a live
    /// swatch and simultaneous Hex/RGB/HSL/HSV/OKLCH readout rows, plus a "pick from image"
    /// section for sampling a color directly off a loaded picture. Separate from the text-based
    /// Color Converter tool, which parses a typed-in color string instead of letting you pick one
    /// visually.
    /// </summary>
    public class ColorPickerControl : UserControl
    {
        private static readonly ColorFormat[] Formats =
        {
            ColorFormat.Hex, ColorFormat.Rgb, ColorFormat.Hsl, ColorFormat.Hsv, ColorFormat.Oklch
        };
        private static readonly string[] FormatLabels = { "HEX", "RGB", "HSL", "HSV", "OKLCH" };

        private readonly ColorGradientBox _gradientBox = new();
        private readonly HueSlider _hueSlider = new();
        private readonly Panel _swatch = new();
        private readonly TextBox[] _txtValues = new TextBox[Formats.Length];

        private readonly Button _btnLoadImage = new();
        private readonly Button _btnRemoveImage = new();
        private readonly Button _btnPickScreen = new();
        private readonly Label _lblImageError = new();
        private readonly PictureBox _imageBox = new();

        private readonly ComboBox _cboHarmony = new();
        private readonly Panel[] _harmonySwatches = new Panel[4];
        private readonly double[] _harmonyHues = new double[4];

        /// <summary>Builds the tool's single card (gradient box, hue slider, swatch, format rows, harmony picker, and the image/screen picker section) and shows the initial color.</summary>
        public ColorPickerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            var card = CardPanel.Add(this, "COLOR PICKER", 0, fill: true);
            BuildPickerCard(card);
            BuildHarmonySection(card);
            BuildImageSection(card);

            UpdateReadout();
        }

        /// <summary>Lays out the gradient box and hue slider on the left, and the swatch plus one readout row per format on the right.</summary>
        private void BuildPickerCard(Panel card)
        {
            _gradientBox.Location = new Point(18, 50);
            _gradientBox.Size = new Size(300, 220);
            _gradientBox.BorderStyle = BorderStyle.FixedSingle;
            _gradientBox.SelectionChanged += (_, _) => UpdateReadout();
            card.Controls.Add(_gradientBox);

            _hueSlider.Location = new Point(18, 282);
            _hueSlider.Size = new Size(300, 24);
            _hueSlider.BorderStyle = BorderStyle.FixedSingle;
            _hueSlider.HueChanged += (_, _) => { _gradientBox.SetHue(_hueSlider.Hue); UpdateReadout(); };
            card.Controls.Add(_hueSlider);

            _swatch.Location = new Point(338, 50);
            _swatch.Size = new Size(120, 120);
            _swatch.BorderStyle = BorderStyle.FixedSingle;
            card.Controls.Add(_swatch);

            for (var i = 0; i < Formats.Length; i++)
            {
                _txtValues[i] = new TextBox();
                AddValueRow(card, FormatLabels[i], _txtValues[i], 338, 190 + i * 46);
            }
        }

        /// <summary>Adds one labeled, read-only result row (HEX/RGB/HSL/HSV/OKLCH) with its own Copy button at the given position.</summary>
        private void AddValueRow(Panel card, string label, TextBox output, int x, int y)
        {
            CardPanel.AddFieldLabel(card, label, x, y);

            output.ReadOnly = true;
            output.Font = Theme.MonoFont;
            output.Location = new Point(x, y + 20);
            output.Width = 180;
            card.Controls.Add(output);

            // A couple of px taller than the textbox's own (auto-computed) height, not an exact
            // match - flush with it clips the descender off letters like "y" (see Color Converter).
            var btnHeight = output.Height + 4;
            var btnCopy = new Button
            {
                Text = "Copy",
                Size = new Size(70, btnHeight),
                Location = new Point(x + 190, y + 20 - (btnHeight - output.Height) / 2)
            };
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += (_, _) =>
            {
                if (output.Text.Length > 0) Clipboard.SetText(output.Text);
            };
            card.Controls.Add(btnCopy);
        }

        /// <summary>Builds the "Harmony" dropdown and its swatch strip, which regenerate from the current hue/saturation/value every time the picked color changes.</summary>
        private void BuildHarmonySection(Panel card)
        {
            CardPanel.AddFieldLabel(card, "Harmony", 18, 316);

            _cboHarmony.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboHarmony.Font = Theme.BaseFont;
            _cboHarmony.Location = new Point(18, 336);
            _cboHarmony.Width = 300;
            // Item order matches the ColorHarmony enum's declaration order - SelectedIndex is
            // cast directly to the enum in UpdateHarmony rather than parsing the text.
            _cboHarmony.Items.AddRange(new object[] { "None", "Complementary", "Analogous", "Triadic", "Split-Complementary", "Tetradic" });
            _cboHarmony.SelectedIndex = 0;
            _cboHarmony.SelectedIndexChanged += (_, _) => UpdateHarmony();
            card.Controls.Add(_cboHarmony);

            for (var i = 0; i < _harmonySwatches.Length; i++)
            {
                var swatch = new Panel
                {
                    Size = new Size(68, 50),
                    Location = new Point(18 + i * 76, 372),
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand,
                    Visible = false
                };
                var index = i;
                // Clicking a harmony swatch jumps the main picker to that hue (keeping the
                // current saturation/value), so harmonies double as quick color-swap shortcuts.
                swatch.Click += (_, _) => ApplyHarmonySwatch(index);
                _harmonySwatches[i] = swatch;
                card.Controls.Add(swatch);
            }
        }

        /// <summary>Recomputes the harmony swatches for the currently selected harmony kind and the picker's current hue/saturation/value.</summary>
        private void UpdateHarmony()
        {
            var offsets = ColorPickerService.HarmonyOffsets((ColorHarmony)_cboHarmony.SelectedIndex);

            for (var i = 0; i < _harmonySwatches.Length; i++)
            {
                if (i >= offsets.Length)
                {
                    _harmonySwatches[i].Visible = false;
                    continue;
                }

                var hue = ColorPickerService.NormalizeHue(_gradientBox.Hue + offsets[i]);
                _harmonyHues[i] = hue;
                var (r, g, b) = ColorPickerService.HsvToRgb(hue, _gradientBox.Saturation, _gradientBox.Value);
                _harmonySwatches[i].BackColor = Color.FromArgb(r, g, b);
                _harmonySwatches[i].Visible = true;
            }
        }

        /// <summary>Sets the gradient box/hue slider to the clicked harmony swatch's hue and refreshes the readout.</summary>
        private void ApplyHarmonySwatch(int index)
        {
            if (!_harmonySwatches[index].Visible) return;
            _hueSlider.SetHue(_harmonyHues[index]);
            _gradientBox.SetHue(_harmonyHues[index]);
            UpdateReadout();
        }

        /// <summary>Builds the "pick from image"/"pick from screen" section: the Load/Remove Image and Pick from Screen buttons, the error label, and the clickable image preview.</summary>
        private void BuildImageSection(Panel card)
        {
            const int sectionY = 430;

            var lblTitle = new Label
            {
                Text = "PICK FROM IMAGE OR SCREEN",
                Font = Theme.SectionFont,
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(18, sectionY)
            };
            card.Controls.Add(lblTitle);

            _btnLoadImage.Text = "Load Image...";
            _btnLoadImage.Location = new Point(18, sectionY + 28);
            _btnLoadImage.Size = new Size(140, 32);
            Theme.StyleSecondaryButton(_btnLoadImage);
            _btnLoadImage.Click += (_, _) => LoadImage();
            card.Controls.Add(_btnLoadImage);

            _btnRemoveImage.Text = "Remove Image";
            _btnRemoveImage.Location = new Point(166, sectionY + 28);
            _btnRemoveImage.Size = new Size(140, 32);
            _btnRemoveImage.Enabled = false;
            Theme.StyleSecondaryButton(_btnRemoveImage);
            _btnRemoveImage.Click += (_, _) => RemoveImage();
            card.Controls.Add(_btnRemoveImage);

            _btnPickScreen.Text = "Pick from Screen";
            _btnPickScreen.Location = new Point(316, sectionY + 28);
            _btnPickScreen.Size = new Size(160, 32);
            Theme.StyleSecondaryButton(_btnPickScreen);
            _btnPickScreen.Click += (_, _) => PickFromScreen();
            card.Controls.Add(_btnPickScreen);

            var lblHint = new Label
            {
                Text = "Click anywhere on the loaded image to pick that pixel's color, or use Pick from Screen to sample any pixel on your monitor.",
                Font = Theme.BaseFont,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Size = new Size(card.Width - 36, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(18, sectionY + 64)
            };
            card.Controls.Add(lblHint);

            _lblImageError.Location = new Point(18, sectionY + 86);
            _lblImageError.Size = new Size(card.Width - 36, 24);
            _lblImageError.ForeColor = Theme.Error;
            _lblImageError.Font = Theme.BaseFont;
            _lblImageError.AutoEllipsis = true;
            _lblImageError.Visible = false;
            card.Controls.Add(_lblImageError);

            _imageBox.Location = new Point(18, sectionY + 114);
            _imageBox.Size = new Size(520, 280);
            _imageBox.BorderStyle = BorderStyle.FixedSingle;
            _imageBox.SizeMode = PictureBoxSizeMode.Zoom;
            _imageBox.BackColor = Theme.Background;
            _imageBox.Cursor = Cursors.Cross;
            _imageBox.MouseClick += (_, e) => PickFromImage(e.Location);
            card.Controls.Add(_imageBox);
        }

        /// <summary>
        /// Hides the app's own window (so it doesn't occlude whatever the user actually wants to
        /// sample from behind it), screenshots the whole virtual desktop, and shows a full-screen
        /// overlay to pick a pixel from that screenshot - see ScreenColorPickerForm for why a
        /// frozen screenshot is used instead of a live global mouse hook.
        /// </summary>
        private void PickFromScreen()
        {
            var owner = FindForm();
            owner?.Hide();
            try
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(150);

                var bounds = SystemInformation.VirtualScreen;
                var capture = new Bitmap(bounds.Width, bounds.Height);
                using (var g = Graphics.FromImage(capture))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                using var overlay = new ScreenColorPickerForm(capture, bounds.Location);
                if (overlay.ShowDialog(owner) == DialogResult.OK)
                {
                    var (h, s, v) = ColorPickerService.RgbToHsv(overlay.PickedColor.R, overlay.PickedColor.G, overlay.PickedColor.B);
                    _hueSlider.SetHue(h);
                    _gradientBox.SetSelection(h, s, v);
                    UpdateReadout();
                }
            }
            finally
            {
                owner?.Show();
                owner?.Activate();
            }
        }

        /// <summary>Prompts for an image file and loads it into the preview, replacing (and disposing) whatever was loaded before.</summary>
        private void LoadImage()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Load Image",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var loaded = new Bitmap(dialog.FileName);
                _imageBox.Image?.Dispose();
                _imageBox.Image = loaded;
                _btnRemoveImage.Enabled = true;
                HideImageError();
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
            {
                ShowImageError($"Could not load image: {ex.Message}");
            }
        }

        /// <summary>Clears and disposes the loaded image, resetting the preview to empty.</summary>
        private void RemoveImage()
        {
            _imageBox.Image?.Dispose();
            _imageBox.Image = null;
            _btnRemoveImage.Enabled = false;
            HideImageError();
        }

        /// <summary>Maps a click location on the image preview to a pixel in the loaded bitmap, samples its color, and feeds it into the gradient box/hue slider/readout.</summary>
        private void PickFromImage(Point clickLocation)
        {
            if (_imageBox.Image is not Bitmap bitmap) return;

            var pixel = MapToImagePixel(bitmap, _imageBox.Size, clickLocation);
            if (pixel is null) return;

            var color = bitmap.GetPixel(pixel.Value.X, pixel.Value.Y);
            var (h, s, v) = ColorPickerService.RgbToHsv(color.R, color.G, color.B);
            _hueSlider.SetHue(h);
            _gradientBox.SetSelection(h, s, v);
            UpdateReadout();
        }

        /// <summary>
        /// Converts a click point inside a Zoom-mode PictureBox into the corresponding pixel
        /// coordinates in the source bitmap, accounting for the letterboxing Zoom applies to
        /// preserve the image's aspect ratio - returns null if the click landed in the letterbox
        /// margin rather than on the image itself.
        /// </summary>
        private static Point? MapToImagePixel(Bitmap bitmap, Size boxSize, Point clickLocation)
        {
            var scale = Math.Min(boxSize.Width / (double)bitmap.Width, boxSize.Height / (double)bitmap.Height);
            var displayedWidth = bitmap.Width * scale;
            var displayedHeight = bitmap.Height * scale;
            var offsetX = (boxSize.Width - displayedWidth) / 2;
            var offsetY = (boxSize.Height - displayedHeight) / 2;

            var relativeX = clickLocation.X - offsetX;
            var relativeY = clickLocation.Y - offsetY;
            if (relativeX < 0 || relativeY < 0 || relativeX >= displayedWidth || relativeY >= displayedHeight) return null;

            var pixelX = Math.Min(bitmap.Width - 1, (int)(relativeX / scale));
            var pixelY = Math.Min(bitmap.Height - 1, (int)(relativeY / scale));
            return new Point(pixelX, pixelY);
        }

        /// <summary>Recomputes the color from the gradient box's current hue/saturation/value and refreshes the swatch and every format row.</summary>
        private void UpdateReadout()
        {
            var (r, g, b) = ColorPickerService.HsvToRgb(_gradientBox.Hue, _gradientBox.Saturation, _gradientBox.Value);
            _swatch.BackColor = Color.FromArgb(r, g, b);

            for (var i = 0; i < Formats.Length; i++)
            {
                _txtValues[i].Text = ColorPickerService.Format(Formats[i], _gradientBox.Hue, _gradientBox.Saturation, _gradientBox.Value);
            }

            UpdateHarmony();
        }

        /// <summary>Shows the given message in the image section's error label.</summary>
        private void ShowImageError(string message)
        {
            _lblImageError.Text = message;
            _lblImageError.Visible = true;
        }

        /// <summary>Hides the image section's error label.</summary>
        private void HideImageError() => _lblImageError.Visible = false;
    }
}
