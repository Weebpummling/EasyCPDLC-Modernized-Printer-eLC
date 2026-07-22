using EasyCPDLC.GNS430;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyCPDLC
{
    public partial class MainForm
    {
        private Gns430Form gns430Panel;

        internal void ShowGns430Panel()
        {
            if (gns430Panel == null || gns430Panel.IsDisposed)
            {
                gns430Panel = new Gns430Form(this);
            }

            if (!gns430Panel.Visible)
            {
                gns430Panel.Show();
            }

            gns430Panel.BringToFront();
        }

        internal Gns430BackendSnapshot GetGns430Snapshot()
        {
            List<Gns430MessageSnapshot> messages = outputTable == null || outputTable.IsDisposed
                ? new List<Gns430MessageSnapshot>()
                : outputTable.Controls
                    .OfType<CPDLCMessage>()
                    .Reverse()
                    .Take(100)
                    .Select(message => new Gns430MessageSnapshot
                    {
                        Source = message,
                        Type = (message.type ?? string.Empty).Trim().ToUpperInvariant(),
                        Station = (message.recipient ?? string.Empty).Trim().ToUpperInvariant(),
                        Text = (message.message ?? string.Empty).Trim(),
                        Outbound = message.outbound,
                        Acknowledged = message.acknowledged,
                        Unread = unreadMessages.Contains(message),
                        Responses = GetGns430Responses(message)
                    })
                    .ToList();

            return new Gns430BackendSnapshot
            {
                Connected = Connected,
                Callsign = (callsign ?? string.Empty).Trim().ToUpperInvariant(),
                CurrentAtcUnit = (CurrentATCUnit ?? string.Empty).Trim().ToUpperInvariant(),
                PendingLogon = (pendingLogon ?? string.Empty).Trim().ToUpperInvariant(),
                Messages = messages
            };
        }

        private static IReadOnlyList<string> GetGns430Responses(CPDLCMessage message)
        {
            if (message == null || message.outbound || message.acknowledged ||
                !string.Equals(message.type, "CPDLC", StringComparison.OrdinalIgnoreCase))
            {
                return new string[0];
            }

            return (message.header?.Responses ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "WU" => new[] { "WILCO", "UNABLE", "STANDBY" },
                "AN" => new[] { "AFFIRMATIVE", "NEGATIVE", "STANDBY" },
                "R" => new[] { "ROGER", "STANDBY" },
                _ => new string[0]
            };
        }

        internal void Gns430ToggleVatsimConnection()
        {
            RetrieveButton_Click(retrieveButton, EventArgs.Empty);
        }

        internal async Task Gns430RequestLogonAsync(string station)
        {
            string cleanStation = (station ?? string.Empty).Trim().ToUpperInvariant();
            if (!Connected)
            {
                WriteMessage("CPDLC LOGON NOT READY: CONNECT TO VATSIM FIRST", "SYSTEM", "SYSTEM");
                return;
            }

            await SendCpdlcLogonRequestAsync(cleanStation, false);
        }

        internal void Gns430Reply(Gns430MessageSnapshot message, string response)
        {
            if (message?.Source == null || message.Source.IsDisposed || string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            MarkMessageRead(message.Source);
            ReplyMessage(EventArgs.Empty, message.Source, response.Trim().ToUpperInvariant());
        }

        internal void Gns430MarkRead(Gns430MessageSnapshot message)
        {
            if (message?.Source != null && !message.Source.IsDisposed)
            {
                MarkMessageRead(message.Source);
            }
        }

        internal void Gns430OpenAtcRequests()
        {
            BringEasyCpdlcWindowToFront();
            BeginInvoke(new Action(() => RequestButton_Click(atcButton, EventArgs.Empty)));
        }

        internal void Gns430OpenAocTelex()
        {
            BringEasyCpdlcWindowToFront();
            BeginInvoke(new Action(() => TelexButton_Click(telexButton, EventArgs.Empty)));
        }

        internal void Gns430OpenSettings()
        {
            BringEasyCpdlcWindowToFront();
            BeginInvoke(new Action(() => SettingsButton_Click(settingsButton, EventArgs.Empty)));
        }
    }
}
