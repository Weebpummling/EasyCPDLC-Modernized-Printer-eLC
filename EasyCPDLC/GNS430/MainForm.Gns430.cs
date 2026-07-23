using EasyCPDLC.GNS430;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyCPDLC
{
    public partial class MainForm
    {
        private Gns430Form gns430Panel;
        private static readonly Dictionary<string, int> Gns430LoadEditionByFlight = new(StringComparer.OrdinalIgnoreCase);

        internal void ShowGns430Panel()
        {
            EnsureGns430Panel();

            if (!gns430Panel.Visible)
            {
                gns430Panel.Show();
            }

            gns430Panel.BringToFront();
        }

        private void EnsureGns430Panel()
        {
            if (gns430Panel == null || gns430Panel.IsDisposed)
            {
                gns430Panel = new Gns430Form(this);
            }
        }

        internal bool SetDcduCompanionMode(bool enabled, out string error)
        {
            EnsureGns430Panel();
            _ = gns430Panel.Handle;
            return gns430Panel.SetDcduCompanionMode(enabled, out error);
        }

        internal bool IsDcduCompanionModeEnabled()
        {
            if (gns430Panel != null && !gns430Panel.IsDisposed)
            {
                return gns430Panel.DcduCompanionMode;
            }

            return Gns430Preferences.Load().DcduCompanionMode;
        }

        private void RestoreGns430CompanionHost()
        {
            Gns430Preferences preferences = Gns430Preferences.Load();
            if (!preferences.CompanionModuleEnabled && !preferences.DcduCompanionMode)
            {
                return;
            }

            EnsureGns430Panel();
            _ = gns430Panel.Handle;
            if (preferences.DcduCompanionMode)
            {
                gns430Panel.SetDcduCompanionMode(true, out _);
            }
        }

        internal void HandleDcduCompanionCommand(Gns430Command command)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => HandleDcduCompanionCommand(command)));
                return;
            }

            if (command >= Gns430Command.DcduLeftLsk1 && command <= Gns430Command.DcduRightLsk6)
            {
                bool rightSide = command >= Gns430Command.DcduRightLsk1;
                int index = rightSide
                    ? (int)command - (int)Gns430Command.DcduRightLsk1 + 1
                    : (int)command - (int)Gns430Command.DcduLeftLsk1 + 1;

                if (IsEmbeddedSetupActive() && IsEmbeddedSetupLineSelectActionAvailable(rightSide, index))
                {
                    HandleEmbeddedSetupLineSelect(rightSide, index);
                }
                else if (previewMessage != null)
                {
                    HandleStyledMessagePreviewLineSelect(rightSide, index);
                }
                else if (IsAirbusAocActive() && index <= 5 && IsAirbusAocLineSelectActionAvailable(rightSide, index))
                {
                    HandleAirbusAocLineSelect(rightSide, index);
                }
                else if (boeingTelexPage != BoeingTelexPage.None && IsBoeingTelexLineSelectActionAvailable(rightSide, index))
                {
                    HandleBoeingTelexLineSelect(rightSide, index);
                }

                return;
            }

            switch (command)
            {
                case Gns430Command.DcduConnect:
                    RetrieveButton_Click(retrieveButton, EventArgs.Empty);
                    break;
                case Gns430Command.DcduAoc:
                    TelexButton_Click(telexButton, EventArgs.Empty);
                    break;
                case Gns430Command.DcduAtc:
                    RequestButton_Click(atcButton, EventArgs.Empty);
                    break;
                case Gns430Command.DcduSettings:
                    SettingsButton_Click(settingsButton, EventArgs.Empty);
                    break;
                case Gns430Command.DcduReloadFlightPlan:
                    ReloadFlightPlanButton_Click(mainReloadFlightPlanButton, EventArgs.Empty);
                    break;
                case Gns430Command.DcduPrint:
                    PrintButton_Click(refreshButtonVisual, EventArgs.Empty);
                    break;
                case Gns430Command.DcduReprint:
                    ReprintButton_Click(boeingReprintButton, EventArgs.Empty);
                    break;
                case Gns430Command.DcduHide:
                    Hide();
                    break;
            }
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
                Departure = AirbusAocDeparture(),
                Arrival = AirbusAocArrival(),
                Aircraft = AirbusAocAircraft(),
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

        internal async Task<Gns430OperationResult> Gns430SendWorkflowAsync(
            Gns430Workflow workflow,
            Gns430BackendSnapshot snapshot)
        {
            if (workflow == null)
            {
                return new Gns430OperationResult { Status = "NO REQUEST SELECTED" };
            }

            string error = workflow.ValidationError();
            if (!string.IsNullOrWhiteSpace(error))
            {
                return new Gns430OperationResult { Status = error };
            }

            if (!Connected)
            {
                return new Gns430OperationResult { Status = "CONNECT VATSIM FIRST" };
            }

            if (workflow.Kind == Gns430WorkflowKind.AocPreDeparture &&
                new[] { snapshot.Callsign, snapshot.Aircraft, snapshot.Departure, snapshot.Arrival }
                    .Any(string.IsNullOrWhiteSpace))
            {
                return new Gns430OperationResult { Status = "LOAD FLIGHT PLAN FOR PDC" };
            }
            if (workflow.Kind == Gns430WorkflowKind.AocOceanic && string.IsNullOrWhiteSpace(snapshot.Callsign))
            {
                return new Gns430OperationResult { Status = "CALLSIGN REQUIRED" };
            }

            string recipient = workflow.Value("RECIPIENT");
            string message = workflow.BuildMessage(snapshot);
            try
            {
                switch (workflow.Kind)
                {
                    case Gns430WorkflowKind.AtcDirect:
                    case Gns430WorkflowKind.AtcLevel:
                    case Gns430WorkflowKind.AtcSpeed:
                    case Gns430WorkflowKind.AtcWhenCanWe:
                    case Gns430WorkflowKind.AtcFreeText:
                        string packet = string.Format("/data2/{0}//Y/{1}", messageOutCounter, message);
                        messageOutCounter += 1;
                        await SendCPDLCMessage(recipient, "CPDLC", packet);
                        break;

                    case Gns430WorkflowKind.AocTelex:
                    case Gns430WorkflowKind.AocPreDeparture:
                        await SendCPDLCMessage(recipient, "TELEX", message);
                        break;

                    case Gns430WorkflowKind.AocOceanic:
                        await SendCPDLCMessage(recipient, "CPDLC", message);
                        break;

                    case Gns430WorkflowKind.AocMetar:
                        recipient = workflow.Value("STATION");
                        WriteMessage("METAR REQUEST", "METAR", recipient, true);
                        ArtificialDelay("METAR " + recipient, "INFOREQ", "REQUEST");
                        break;

                    case Gns430WorkflowKind.AocAtis:
                        string station = workflow.Value("STATION");
                        if (!TryResolveAtisRequestTarget(station, workflow.Value("TYPE"), out recipient, out string warning))
                        {
                            return new Gns430OperationResult { Status = warning };
                        }
                        SetAtisAutoRefresh(recipient, workflow.Value("AUTO") == "ON");
                        WriteMessage("ATIS REQUEST", "ATIS", recipient, true);
                        ArtificialDelay("VATATIS " + recipient, "INFOREQ", "REQUEST");
                        _ = RefreshVatsimForAtisHoverAsync();
                        break;
                }

                return new Gns430OperationResult { Success = true, Status = "REQUEST SENT" };
            }
            catch (Exception ex)
            {
                Logger.Debug("GNS 430 request failed: " + ex.Message);
                return new Gns430OperationResult { Status = SafeGns430Error(ex) };
            }
        }

        internal async Task<Gns430LoadControlSession> Gns430PrepareLoadControlAsync()
        {
            string key = SavedELoadControlApiKey;
            string simbriefUser = SimbriefID;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("SAVE ELOADCONTROL API KEY IN SETUP FIRST.");
            }
            if (string.IsNullOrWhiteSpace(simbriefUser))
            {
                throw new InvalidOperationException("SAVE SIMBRIEF USER IN SETUP FIRST.");
            }

            SimbriefLoadsheetData flight = await new SimbriefLoadsheetClient()
                .FetchAsync(simbriefUser, callsign ?? string.Empty, CancellationToken.None);
            ELoadReferenceData reference = await new ELoadControlClient()
                .GetReferenceDataAsync(key, flight.AircraftIcao, CancellationToken.None);

            List<ELoadAircraft> usableAircraft = reference.Aircraft
                .Where(item => item.CabinConfigurations.Count > 0)
                .ToList();
            if (usableAircraft.Count == 0)
            {
                throw new InvalidOperationException("ELOADCONTROL HAS NO USABLE AIRCRAFT MATCH.");
            }
            if (reference.Formats.Count == 0)
            {
                throw new InvalidOperationException("ELOADCONTROL RETURNED NO LOADSHEET FORMATS.");
            }

            ELoadReferenceData usable = new() { Aircraft = usableAircraft, Formats = reference.Formats };
            int aircraftIndex = usableAircraft.FindIndex(item =>
                string.Equals(item.Icao, flight.AircraftIcao, StringComparison.OrdinalIgnoreCase));
            Gns430LoadControlSession session = new()
            {
                Flight = flight,
                Reference = usable,
                AircraftIndex = Math.Max(0, aircraftIndex)
            };
            session.RebuildPassengerSplit();
            return session;
        }

        internal async Task<Gns430OperationResult> Gns430GenerateLoadsheetAsync(Gns430LoadControlSession session)
        {
            if (session?.Flight == null)
            {
                return new Gns430OperationResult { Status = "LOAD DATA NOT READY" };
            }
            if (session.PassengerSplit.Sum(item => item.Passengers) != session.Flight.PassengerCount)
            {
                return new Gns430OperationResult { Status = "PAX TOTAL MUST BE " + session.Flight.PassengerCount };
            }

            try
            {
                int edition = Gns430LoadEditionByFlight.TryGetValue(session.Flight.FlightKey, out int last)
                    ? Math.Max(1, last + 1)
                    : 1;
                Newtonsoft.Json.Linq.JObject request = session.Flight.BuildGenerateRequest(
                    session.Cabin,
                    session.Format.TemplateId,
                    session.PassengerSplit,
                    edition,
                    session.Aircraft.Icao);
                ELoadLoadsheetResult result = await new ELoadControlClient()
                    .GenerateLoadsheetAsync(SavedELoadControlApiKey, request, CancellationToken.None);
                Gns430LoadEditionByFlight[session.Flight.FlightKey] = Math.Max(edition, result.EditionNumber);
                ReceiveELoadControlLoadsheet(result, session.Flight, false);
                return new Gns430OperationResult { Success = true, Status = "LOADSHEET RECEIVED" };
            }
            catch (Exception ex)
            {
                Logger.Debug("GNS 430 eLoadControl failed: " + ex.Message);
                return new Gns430OperationResult { Status = SafeGns430Error(ex) };
            }
        }

        private static string SafeGns430Error(Exception ex)
        {
            string text = (ex?.Message ?? "REQUEST FAILED").Trim().ToUpperInvariant();
            return text.Length <= 80 ? text : text.Substring(0, 80);
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
