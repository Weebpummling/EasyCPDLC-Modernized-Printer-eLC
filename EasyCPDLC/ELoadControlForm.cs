using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyCPDLC
{
    internal sealed class ELoadControlForm : Form
    {
        private sealed class Choice<T>
        {
            public string Text { get; init; } = string.Empty;
            public T Value { get; init; }
            public override string ToString() => Text;
        }

        private static readonly Dictionary<string, int> LastEditionByFlight = new(StringComparer.OrdinalIgnoreCase);
        private readonly MainForm owner;
        private readonly string fallbackCallsign;
        private readonly SimbriefLoadsheetClient simbriefClient = new();
        private readonly ELoadControlClient eloadClient = new();
        private readonly CancellationTokenSource cancellation = new();
        private readonly TextBox apiKeyBox = new();
        private readonly TextBox simbriefBox = new();
        private readonly CheckBox rememberKeyBox = new();
        private readonly ComboBox aircraftBox = new();
        private readonly ComboBox cabinBox = new();
        private readonly ComboBox formatBox = new();
        private readonly FlowLayoutPanel splitPanel = new();
        private readonly Label flightSummary = new();
        private readonly Label splitSummary = new();
        private readonly Label statusLabel = new();
        private readonly Button loadButton = new();
        private readonly Button generateButton = new();
        private readonly Dictionary<string, NumericUpDown> splitInputs = new(StringComparer.OrdinalIgnoreCase);
        private SimbriefLoadsheetData flight;
        private ELoadReferenceData referenceData;
        private bool busy;

        public ELoadControlForm(MainForm owner, string fallbackCallsign)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.fallbackCallsign = (fallbackCallsign ?? string.Empty).Trim().ToUpperInvariant();

            Text = "EasyCPDLC — eLoadControl Loadsheet";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 650);
            ClientSize = new Size(760, 690);
            BackColor = Color.FromArgb(3, 9, 14);
            ForeColor = Color.FromArgb(114, 235, 255);
            Font = new Font("Consolas", 10.0f, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;

            BuildInterface();
            apiKeyBox.Text = MainForm.SavedELoadControlApiKey;
            simbriefBox.Text = MainForm.SimbriefID;
            FormClosed += (_, __) => cancellation.Cancel();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                ColumnCount = 2,
                RowCount = 13,
                BackColor = BackColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            Controls.Add(root);

            Label title = NewLabel("AOC LOADSHEET — ELOADCONTROL", Color.WhiteSmoke, 14.5f, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleCenter;
            root.Controls.Add(title, 0, 0);
            root.SetColumnSpan(title, 2);

            root.Controls.Add(NewLabel("PRO API KEY", ForeColor, 10.0f, FontStyle.Bold), 0, 1);
            ConfigureTextBox(apiKeyBox, true);
            root.Controls.Add(apiKeyBox, 1, 1);

            root.Controls.Add(NewLabel("SIMBRIEF USER", ForeColor, 10.0f, FontStyle.Bold), 0, 2);
            ConfigureTextBox(simbriefBox, false);
            root.Controls.Add(simbriefBox, 1, 2);

            rememberKeyBox.Text = "SAVE KEY FOR THIS WINDOWS USER (DPAPI ENCRYPTED)";
            rememberKeyBox.Checked = true;
            rememberKeyBox.AutoSize = true;
            rememberKeyBox.ForeColor = Color.FromArgb(210, 220, 224);
            rememberKeyBox.Anchor = AnchorStyles.Left;
            root.Controls.Add(rememberKeyBox, 1, 3);

            loadButton.Text = "LOAD SIMBRIEF + AVAILABLE CONFIGS";
            StyleButton(loadButton, Color.FromArgb(15, 58, 68));
            loadButton.Click += async (_, __) => await LoadFlightAsync();
            root.Controls.Add(loadButton, 0, 3);

            flightSummary.Text = "Load the latest SimBrief OFP, then review the selected aircraft and cabin configuration.";
            flightSummary.ForeColor = Color.FromArgb(185, 198, 204);
            flightSummary.AutoEllipsis = true;
            flightSummary.TextAlign = ContentAlignment.MiddleLeft;
            flightSummary.Dock = DockStyle.Fill;
            root.Controls.Add(flightSummary, 0, 4);
            root.SetColumnSpan(flightSummary, 2);

            root.Controls.Add(NewLabel("AIRCRAFT", ForeColor, 10.0f, FontStyle.Bold), 0, 5);
            ConfigureCombo(aircraftBox);
            aircraftBox.SelectedIndexChanged += (_, __) => PopulateCabins();
            root.Controls.Add(aircraftBox, 1, 5);

            root.Controls.Add(NewLabel("CABIN CONFIG", ForeColor, 10.0f, FontStyle.Bold), 0, 6);
            ConfigureCombo(cabinBox);
            cabinBox.SelectedIndexChanged += (_, __) => BuildPassengerSplit();
            root.Controls.Add(cabinBox, 1, 6);

            root.Controls.Add(NewLabel("LOADSHEET FORMAT", ForeColor, 10.0f, FontStyle.Bold), 0, 7);
            ConfigureCombo(formatBox);
            root.Controls.Add(formatBox, 1, 7);

            splitSummary.Text = "PASSENGER ALLOCATION";
            splitSummary.ForeColor = Color.FromArgb(255, 191, 64);
            splitSummary.Font = new Font(Font.FontFamily, 9.6f, FontStyle.Bold);
            splitSummary.Dock = DockStyle.Fill;
            splitSummary.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(splitSummary, 0, 8);
            root.SetColumnSpan(splitSummary, 2);

            splitPanel.Dock = DockStyle.Fill;
            splitPanel.AutoScroll = true;
            splitPanel.WrapContents = true;
            splitPanel.Padding = new Padding(2, 6, 2, 6);
            splitPanel.BackColor = Color.FromArgb(1, 6, 10);
            root.Controls.Add(splitPanel, 0, 9);
            root.SetColumnSpan(splitPanel, 2);

            statusLabel.Text = "READY — no loadsheet request has been sent.";
            statusLabel.ForeColor = Color.FromArgb(185, 198, 204);
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(statusLabel, 0, 10);
            root.SetColumnSpan(statusLabel, 2);

            FlowLayoutPanel actions = new()
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                WrapContents = false
            };
            Button closeButton = new() { Text = "CLOSE", Width = 110, Height = 34 };
            StyleButton(closeButton, Color.FromArgb(46, 48, 52));
            closeButton.Click += (_, __) => Close();
            generateButton.Text = "CONFIRM + GENERATE";
            generateButton.Width = 200;
            generateButton.Height = 34;
            generateButton.Enabled = false;
            StyleButton(generateButton, Color.FromArgb(12, 91, 61));
            generateButton.Click += async (_, __) => await GenerateAsync();
            actions.Controls.Add(closeButton);
            actions.Controls.Add(generateButton);
            root.Controls.Add(actions, 0, 11);
            root.SetColumnSpan(actions, 2);

            Label boundary = NewLabel(
                "Independent AOC source: this screen does not change the PDC badge, DCL logon, or REQ CLR availability.",
                Color.FromArgb(142, 155, 160), 8.5f, FontStyle.Regular);
            boundary.TextAlign = ContentAlignment.MiddleCenter;
            root.Controls.Add(boundary, 0, 12);
            root.SetColumnSpan(boundary, 2);
        }

        private static Label NewLabel(string text, Color color, float size, FontStyle style)
        {
            return new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("Consolas", size, style),
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static void ConfigureTextBox(TextBox box, bool password)
        {
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0, 5, 0, 5);
            box.BackColor = Color.FromArgb(2, 18, 24);
            box.ForeColor = Color.WhiteSmoke;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.UseSystemPasswordChar = password;
        }

        private static void ConfigureCombo(ComboBox box)
        {
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0, 4, 0, 4);
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.FlatStyle = FlatStyle.Popup;
            box.BackColor = Color.FromArgb(2, 18, 24);
            box.ForeColor = Color.WhiteSmoke;
        }

        private static void StyleButton(Button button, Color color)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(103, 181, 194);
            button.BackColor = color;
            button.ForeColor = Color.WhiteSmoke;
            button.Font = new Font("Consolas", 9.2f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        private async Task LoadFlightAsync()
        {
            if (busy)
            {
                return;
            }

            string key = apiKeyBox.Text.Trim();
            string simbriefUser = simbriefBox.Text.Trim();
            if (key.Length == 0 || simbriefUser.Length == 0)
            {
                ShowError("Enter both the eLoadControl Pro API key and SimBrief username.");
                return;
            }

            try
            {
                SetBusy(true, "LOADING SIMBRIEF OFP...");
                SaveCredentials(key, simbriefUser);
                flight = await simbriefClient.FetchAsync(simbriefUser, fallbackCallsign, cancellation.Token);

                SetStatus("LOADING ELOADCONTROL AIRCRAFT + FORMAT REFERENCES...");
                referenceData = await eloadClient.GetReferenceDataAsync(key, flight.AircraftIcao, cancellation.Token);
                if (referenceData.Aircraft.Count == 0)
                {
                    throw new InvalidOperationException("ELOADCONTROL HAS NO AIRCRAFT MATCH FOR " + flight.AircraftIcao + ".");
                }
                if (referenceData.Formats.Count == 0)
                {
                    throw new InvalidOperationException("ELOADCONTROL RETURNED NO LOADSHEET FORMATS.");
                }

                aircraftBox.Items.Clear();
                foreach (ELoadAircraft item in referenceData.Aircraft)
                {
                    aircraftBox.Items.Add(new Choice<ELoadAircraft>
                    {
                        Text = item.Icao + " — " + item.Type,
                        Value = item
                    });
                }

                formatBox.Items.Clear();
                foreach (ELoadFormat item in referenceData.Formats)
                {
                    formatBox.Items.Add(new Choice<ELoadFormat>
                    {
                        Text = item.Name + " (" + item.TemplateId + ")",
                        Value = item
                    });
                }

                int exactAircraft = referenceData.Aircraft
                    .Select((item, index) => new { item, index })
                    .FirstOrDefault(value => string.Equals(value.item.Icao, flight.AircraftIcao, StringComparison.OrdinalIgnoreCase))?.index ?? 0;
                aircraftBox.SelectedIndex = exactAircraft;
                formatBox.SelectedIndex = 0;
                flightSummary.Text =
                    (flight.Airline + flight.FlightNumber + "  " + flight.Departure + "–" + flight.Destination +
                    "  " + flight.AircraftRegistration + "  PAX " + flight.PassengerCount).Trim();
                generateButton.Enabled = splitInputs.Count > 0;
                SetStatus("REVIEW THE PREFILLED PASSENGER ALLOCATION, THEN CONFIRM GENERATION.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                flight = null;
                referenceData = null;
                generateButton.Enabled = false;
                SetStatus(SafeError(ex));
                ShowError(SafeError(ex));
            }
            finally
            {
                SetBusy(false, statusLabel.Text);
            }
        }

        private void SaveCredentials(string key, string simbriefUser)
        {
            MainForm.SimbriefID = simbriefUser;
            MainForm.SavedELoadControlApiKey = rememberKeyBox.Checked ? key : string.Empty;
            Properties.Settings.Default.Save();
        }

        private void PopulateCabins()
        {
            cabinBox.Items.Clear();
            splitPanel.Controls.Clear();
            splitInputs.Clear();

            if (aircraftBox.SelectedItem is not Choice<ELoadAircraft> selected)
            {
                return;
            }

            foreach (string cabin in selected.Value.CabinConfigurations)
            {
                cabinBox.Items.Add(cabin);
            }
            if (cabinBox.Items.Count > 0)
            {
                cabinBox.SelectedIndex = 0;
            }
        }

        private void BuildPassengerSplit()
        {
            splitPanel.Controls.Clear();
            splitInputs.Clear();
            if (flight == null || cabinBox.SelectedItem is not string cabin)
            {
                generateButton.Enabled = false;
                return;
            }

            IReadOnlyList<PassengerClassAllocation> allocation = ELoadPassengerSplitter.Split(cabin, flight.PassengerCount);
            splitSummary.Text = allocation.Count == 1
                ? "SINGLE CLASS — CONFIRM TOTAL PAX " + flight.PassengerCount
                : "AUTO SPLIT — EDIT IF NEEDED; TOTAL MUST REMAIN " + flight.PassengerCount;

            foreach (PassengerClassAllocation item in allocation)
            {
                Panel card = new()
                {
                    Width = 210,
                    Height = 62,
                    Margin = new Padding(7),
                    BackColor = Color.FromArgb(5, 27, 34)
                };
                Label label = NewLabel(item.Code + "  CAP " + item.Capacity, ForeColor, 9.2f, FontStyle.Bold);
                label.Location = new Point(10, 5);
                label.Size = new Size(100, 24);
                label.Dock = DockStyle.None;
                NumericUpDown input = new()
                {
                    Minimum = 0,
                    Maximum = 999,
                    Value = Math.Min(999, item.Passengers),
                    Location = new Point(112, 12),
                    Size = new Size(82, 30),
                    BackColor = Color.FromArgb(2, 18, 24),
                    ForeColor = Color.WhiteSmoke,
                    Font = new Font("Consolas", 10.5f, FontStyle.Bold)
                };
                input.ValueChanged += (_, __) => UpdateSplitStatus();
                card.Controls.Add(label);
                card.Controls.Add(input);
                splitPanel.Controls.Add(card);
                splitInputs[item.Code] = input;
            }

            generateButton.Enabled = splitInputs.Count > 0;
            UpdateSplitStatus();
        }

        private void UpdateSplitStatus()
        {
            if (flight == null)
            {
                return;
            }
            int total = splitInputs.Values.Sum(input => (int)input.Value);
            splitSummary.ForeColor = total == flight.PassengerCount
                ? Color.FromArgb(97, 238, 154)
                : Color.FromArgb(255, 126, 96);
        }

        private async Task GenerateAsync()
        {
            if (busy || flight == null ||
                aircraftBox.SelectedItem is not Choice<ELoadAircraft> aircraft ||
                formatBox.SelectedItem is not Choice<ELoadFormat> format ||
                cabinBox.SelectedItem is not string cabin)
            {
                return;
            }

            List<PassengerClassAllocation> split = splitInputs.Select(pair => new PassengerClassAllocation
            {
                Code = pair.Key,
                Capacity = ELoadPassengerSplitter.Split(cabin, flight.PassengerCount)
                    .FirstOrDefault(item => string.Equals(item.Code, pair.Key, StringComparison.OrdinalIgnoreCase))?.Capacity ?? 0,
                Passengers = (int)pair.Value.Value
            }).ToList();

            int total = split.Sum(item => item.Passengers);
            if (total != flight.PassengerCount)
            {
                ShowError("Passenger allocation totals " + total + ", but SimBrief contains " + flight.PassengerCount + ".");
                return;
            }

            bool overCapacity = split.Any(item => item.Capacity > 0 && item.Passengers > item.Capacity);
            string confirmation =
                "Generate edition " + NextEdition(flight.FlightKey) + " for " + flight.Airline + flight.FlightNumber + "?\r\n\r\n" +
                string.Join("   ", split.Select(item => item.Code + " " + item.Passengers + "/" + item.Capacity)) +
                (overCapacity ? "\r\n\r\nWARNING: one or more classes exceed configured seat capacity." : string.Empty) +
                "\r\n\r\nThis sends one loadsheet-generation request to eLoadControl.";
            if (MessageBox.Show(this, confirmation, "CONFIRM ELOADCONTROL REQUEST", MessageBoxButtons.YesNo,
                overCapacity ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                SetBusy(true, "GENERATING LOADSHEET — PLEASE WAIT...");
                string key = apiKeyBox.Text.Trim();
                SaveCredentials(key, simbriefBox.Text.Trim());
                int edition = NextEdition(flight.FlightKey);
                JObject request = flight.BuildGenerateRequest(cabin, format.Value.TemplateId, split, edition, aircraft.Value.Icao);
                ELoadLoadsheetResult result = await eloadClient.GenerateLoadsheetAsync(key, request, cancellation.Token);
                LastEditionByFlight[flight.FlightKey] = Math.Max(edition, result.EditionNumber);
                owner.ReceiveELoadControlLoadsheet(result, flight);
                SetStatus("LOADSHEET RECEIVED — OPENED FOR REVIEW; USE THE ON-SCREEN PRINT ACTION WHEN READY.");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                SetStatus(SafeError(ex));
                ShowError(SafeError(ex));
            }
            finally
            {
                SetBusy(false, statusLabel.Text);
            }
        }

        private static int NextEdition(string flightKey)
        {
            return LastEditionByFlight.TryGetValue(flightKey ?? string.Empty, out int last)
                ? Math.Max(1, last + 1)
                : 1;
        }

        private void SetBusy(bool value, string status)
        {
            busy = value;
            loadButton.Enabled = !value;
            generateButton.Enabled = !value && flight != null && splitInputs.Count > 0;
            UseWaitCursor = value;
            SetStatus(status);
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = (text ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string SafeError(Exception ex)
        {
            if (ex is ELoadControlException apiError && apiError.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return "ELOADCONTROL MONTHLY API QUOTA EXCEEDED.";
            }
            string message = (ex?.Message ?? "REQUEST FAILED.").Replace('\r', ' ').Replace('\n', ' ').Trim();
            return message.Length <= 240 ? message : message.Substring(0, 237) + "...";
        }

        private void ShowError(string text)
        {
            MessageBox.Show(this, text, "ELOADCONTROL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
