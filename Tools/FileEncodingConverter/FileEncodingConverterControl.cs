using System.Text;
using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.FileEncodingConverter
{
    public class FileEncodingConverterControl : UserControl
    {
        private readonly Button _btnChooseFile = new();
        private readonly Label _lblFileName = new();
        private readonly ComboBox _cboSourceEncoding = new();
        private readonly ComboBox _cboTargetEncoding = new();
        private readonly Button _btnConvertSave = new();
        private readonly Label _lblStatus = new();
        private readonly TextBox _txtPreview = new();

        private string? _sourceFilePath;

        public FileEncodingConverterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top cards below it - see the docking
            // order note in TextTransformControl/JsonFormatterControl. Same-edge Dock=Top
            // siblings stack in reverse add-order, so the source-file card (added last) ends
            // up visually on top, above the target-encoding card.
            BuildPreviewCard();
            BuildTargetCard();
            BuildSourceCard();
        }

        private void BuildPreviewCard()
        {
            var card = CardPanel.Add(this, "DECODED PREVIEW (using source encoding)", 0, fill: true);
            _txtPreview.Multiline = true;
            _txtPreview.ReadOnly = true;
            _txtPreview.ScrollBars = ScrollBars.Vertical;
            CardPanel.WrapWithBorder(card, _txtPreview, new Point(18, 42), card.Width - 36, card.Height - 58,
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
        }

        private void BuildSourceCard()
        {
            var card = CardPanel.Add(this, "CHOOSE FILE & SOURCE ENCODING", 190);

            _btnChooseFile.Text = "Choose File...";
            _btnChooseFile.UseMnemonic = false;
            _btnChooseFile.Location = new Point(18, 44);
            _btnChooseFile.Size = new Size(140, 32);
            Theme.StyleSecondaryButton(_btnChooseFile);
            _btnChooseFile.Click += OnChooseFileClick;
            card.Controls.Add(_btnChooseFile);

            _lblFileName.Text = "No file chosen";
            _lblFileName.ForeColor = Theme.TextMuted;
            _lblFileName.Font = Theme.BaseFont;
            _lblFileName.AutoEllipsis = true;
            _lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            _lblFileName.Location = new Point(170, 44);
            _lblFileName.Size = new Size(card.Width - 36 - 170, 32);
            _lblFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(_lblFileName);

            CardPanel.AddFieldLabel(card, "Source encoding", 18, 90);
            _cboSourceEncoding.Location = new Point(18, 108);
            _cboSourceEncoding.Width = 320;
            _cboSourceEncoding.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboSourceEncoding.Font = Theme.BaseFont;
            PopulateEncodings(_cboSourceEncoding);
            _cboSourceEncoding.SelectedIndexChanged += OnSourceEncodingChanged;
            card.Controls.Add(_cboSourceEncoding);
        }

        private void BuildTargetCard()
        {
            var card = CardPanel.Add(this, "TARGET ENCODING & SAVE", 190);

            CardPanel.AddFieldLabel(card, "Target encoding", 18, 44);
            _cboTargetEncoding.Location = new Point(18, 64);
            _cboTargetEncoding.Width = 320;
            _cboTargetEncoding.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboTargetEncoding.Font = Theme.BaseFont;
            PopulateEncodings(_cboTargetEncoding);
            card.Controls.Add(_cboTargetEncoding);

            _btnConvertSave.Text = "Convert & Save As...";
            _btnConvertSave.UseMnemonic = false;
            _btnConvertSave.Location = new Point(18, 104);
            _btnConvertSave.Size = new Size(180, 32);
            Theme.StylePrimaryButton(_btnConvertSave);
            _btnConvertSave.Click += OnConvertSaveClick;
            card.Controls.Add(_btnConvertSave);

            _lblStatus.Location = new Point(18, 146);
            _lblStatus.Size = new Size(card.Width - 36, 34);
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblStatus.Font = Theme.BaseFont;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.Visible = false;
            card.Controls.Add(_lblStatus);
        }

        private static void PopulateEncodings(ComboBox combo)
        {
            foreach (var (display, encoding) in EncodingCatalog.Available)
            {
                combo.Items.Add(new EncodingItem(display, encoding));
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void OnChooseFileClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose a file to convert",
                Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt"
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _sourceFilePath = dialog.FileName;
            _lblFileName.Text = Path.GetFileName(_sourceFilePath);
            LoadPreview();
        }

        private void OnSourceEncodingChanged(object? sender, EventArgs e)
        {
            if (_sourceFilePath is not null) LoadPreview();
        }

        private void LoadPreview()
        {
            try
            {
                var sourceEncoding = SelectedEncoding(_cboSourceEncoding);
                _txtPreview.Text = FileEncodingConverterService.ReadText(_sourceFilePath!, sourceEncoding);
                HideStatus();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowStatus($"Could not read file: {ex.Message}", isError: true);
            }
        }

        private void OnConvertSaveClick(object? sender, EventArgs e)
        {
            if (_sourceFilePath is null)
            {
                ShowStatus("Choose a source file first.", isError: true);
                return;
            }

            try
            {
                var sourceEncoding = SelectedEncoding(_cboSourceEncoding);
                var targetEncoding = SelectedEncoding(_cboTargetEncoding);
                var text = FileEncodingConverterService.ReadText(_sourceFilePath, sourceEncoding);

                using var dialog = new SaveFileDialog
                {
                    Title = "Save converted file as",
                    FileName = Path.GetFileName(_sourceFilePath),
                    Filter = "All files (*.*)|*.*"
                };
                if (dialog.ShowDialog() != DialogResult.OK) return;

                FileEncodingConverterService.WriteText(dialog.FileName, text, targetEncoding);
                ShowStatus($"Saved to {dialog.FileName}", isError: false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Conversion failed: {ex.Message}", isError: true);
            }
        }

        private static Encoding SelectedEncoding(ComboBox combo) =>
            (combo.SelectedItem as EncodingItem)?.Encoding ?? EncodingCatalog.Default;

        private void ShowStatus(string message, bool isError)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = isError ? Theme.Error : Theme.Success;
            _lblStatus.Visible = true;
        }

        private void HideStatus() => _lblStatus.Visible = false;

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
    }
}
