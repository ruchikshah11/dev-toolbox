using DevToolbox.UI;

namespace DevToolbox.Tools.TimezoneConverter
{
    public class TimezoneConverterControl : UserControl
    {
        private readonly DateTimePicker _dtPicker = new();
        private readonly Button _btnNow = new();
        private readonly ComboBox _cboSource = new();
        private readonly Label _lblError = new();
        private readonly ReferenceTableControl _table = new("ALL TIMEZONES", new[] { "Zone", "Local Time", "UTC Offset" }, Array.Empty<string[]>());

        /// <summary>Builds the date/time + source-zone input bar and the all-zones searchable table.</summary>
        public TimezoneConverterControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Dock=Fill must be added before the Dock=Top card below it - see the docking order
            // note used throughout the tool controls.
            Controls.Add(_table);
            BuildInputCard();

            RefreshTable();
        }

        /// <summary>Builds the date/time picker, source-zone dropdown, Now button, and error label.</summary>
        private void BuildInputCard()
        {
            var card = CardPanel.Add(this, "DATE/TIME & SOURCE ZONE", 130);
            const int labelY = 44, fieldY = 64;

            var lblWhen = CardPanel.AddFieldLabel(card, "Date/Time", 18, labelY);
            _dtPicker.Format = DateTimePickerFormat.Custom;
            _dtPicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            _dtPicker.ShowUpDown = true;
            _dtPicker.Font = Theme.MonoFont;
            _dtPicker.Location = new Point(18, fieldY);
            _dtPicker.Width = 220;
            _dtPicker.ValueChanged += (_, _) => RefreshTable();
            card.Controls.Add(_dtPicker);

            _btnNow.Text = "Now";
            _btnNow.Size = new Size(70, 28);
            _btnNow.Location = new Point(246, fieldY - 1);
            Theme.StyleSecondaryButton(_btnNow);
            _btnNow.Click += (_, _) => SetToNow();
            card.Controls.Add(_btnNow);

            var lblSource = CardPanel.AddFieldLabel(card, "Source Zone (what this date/time is in)", 0, labelY);
            lblSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cboSource.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboSource.Font = Theme.BaseFont;
            _cboSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            foreach (var zone in TimezoneConverterService.AllZones) _cboSource.Items.Add(new ZoneItem(zone));
            SelectZone(TimeZoneInfo.Local);
            _cboSource.SelectedIndexChanged += (_, _) => RefreshTable();
            card.Controls.Add(_cboSource);

            void PositionSourceZone()
            {
                var comboWidth = 360;
                _cboSource.Width = comboWidth;
                _cboSource.Location = new Point(card.Width - 18 - comboWidth, fieldY);
                lblSource.Location = new Point(card.Width - 18 - lblSource.Width, labelY);
            }
            card.Resize += (_, _) => PositionSourceZone();
            PositionSourceZone();

            _lblError.Location = new Point(18, 100);
            _lblError.Size = new Size(card.Width - 36, 24);
            _lblError.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _lblError.ForeColor = Theme.Error;
            _lblError.Font = Theme.BaseFont;
            _lblError.AutoEllipsis = true;
            _lblError.Visible = false;
            card.Controls.Add(_lblError);
        }

        /// <summary>Sets the picker to the current instant, expressed as wall-clock time in the selected source zone.</summary>
        private void SetToNow() => _dtPicker.Value = TimezoneConverterService.NowInZone(SelectedZone());

        /// <summary>Selects the given zone in the source dropdown, if present.</summary>
        private void SelectZone(TimeZoneInfo zone)
        {
            for (var i = 0; i < _cboSource.Items.Count; i++)
            {
                if (((ZoneItem)_cboSource.Items[i]).Zone.Id != zone.Id) continue;
                _cboSource.SelectedIndex = i;
                return;
            }
        }

        /// <summary>Reads back the currently selected source zone.</summary>
        private TimeZoneInfo SelectedZone() => (_cboSource.SelectedItem as ZoneItem)?.Zone ?? TimeZoneInfo.Local;

        /// <summary>Recomputes every zone's converted time for the current inputs and refreshes the table.</summary>
        private void RefreshTable()
        {
            try
            {
                var results = TimezoneConverterService.ConvertToAllZones(_dtPicker.Value, SelectedZone());
                _table.SetRows(results.Select(r => new[]
                {
                    r.DisplayName,
                    r.LocalTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    TimezoneConverterService.FormatOffset(r.UtcOffset)
                }));
                HideError();
            }
            catch (Exception ex)
            {
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

        private sealed class ZoneItem
        {
            public ZoneItem(TimeZoneInfo zone) => Zone = zone;

            public TimeZoneInfo Zone { get; }

            public override string ToString() => Zone.DisplayName;
        }
    }
}
