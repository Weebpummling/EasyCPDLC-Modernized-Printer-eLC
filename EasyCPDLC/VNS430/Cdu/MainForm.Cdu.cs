using EasyCPDLC.VNS430;
using EasyCPDLC.VNS430.Cdu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EasyCPDLC
{
    // LSK-only "CDU" display mode. A third DcduStyle alongside Airbus/Boeing that renders a
    // 28x12 character grid driven entirely by the twelve line-select keys (clickable on
    // screen and driveable by the MobiFlight EASYCPDLC_DCDU_LSK_* L-vars). It reuses the
    // same backend as the DCDU skins through the existing Vns430* wrappers and leaves the
    // Airbus/Boeing code paths untouched.
    public partial class MainForm
    {
        private enum CduPageId
        {
            Menu,
            Dlk,
            Messages,
            MessageDetail,
            Atc,
            Aoc,
            Request
        }

        private CduDisplayPanel cduDisplayPanel;
        private System.Windows.Forms.Timer cduRefreshTimer;
        private CduPageId cduPage = CduPageId.Menu;
        private CPDLCMessage cduSelectedMessage;
        private readonly List<CPDLCMessage> cduVisibleInbox = new();

        // Request-page (ATC/AOC) state and the shared scratchpad.
        private Vns430Workflow cduWorkflow;
        private string cduScratchpad = string.Empty;
        private string cduStatusLine = string.Empty;
        private bool cduRequestSending;

        internal bool IsCduModeActive() => DcduStyleManager.IsCdu;

        // Entry point from ApplyDisplayStyle when the CDU style is selected.
        private void MountCduMode()
        {
            if (dcduFrame == null)
            {
                return;
            }

            if (cduDisplayPanel == null || cduDisplayPanel.IsDisposed)
            {
                cduDisplayPanel = new CduDisplayPanel { Dock = DockStyle.Fill };
                cduDisplayPanel.LskPressed += CduDisplayPanel_LskPressed;
                cduDisplayPanel.CharTyped += (_, c) => CduScratchpadType(c);
                cduDisplayPanel.ScratchpadBackspace += (_, __) => CduScratchpadBackspace();
                cduDisplayPanel.ScratchpadClear += (_, __) => CduScratchpadClearAll();
                dcduFrame.Controls.Add(cduDisplayPanel);
            }

            // Hide every Airbus/Boeing element; the CDU panel is the whole screen.
            SetAirbusAocChromeVisible(false);
            if (screenPanel != null) screenPanel.Visible = false;
            if (messageFormatPanel != null) messageFormatPanel.Visible = false;
            dcduFrame.ShowArtwork = false;
            dcduFrame.BackColor = Color.Black;

            cduDisplayPanel.Visible = true;
            cduDisplayPanel.BringToFront();

            if (cduRefreshTimer == null)
            {
                cduRefreshTimer = new System.Windows.Forms.Timer { Interval = 750 };
                cduRefreshTimer.Tick += (_, __) =>
                {
                    if (IsCduModeActive())
                    {
                        RefreshCduDisplay();
                    }
                };
            }
            cduRefreshTimer.Start();

            cduPage = CduPageId.Menu;
            RefreshCduDisplay();
        }

        // Called from ApplyDisplayStyle's non-CDU branch so switching back to Airbus/Boeing
        // restores the normal chrome.
        private void TeardownCduMode()
        {
            cduRefreshTimer?.Stop();
            if (cduDisplayPanel != null)
            {
                cduDisplayPanel.Visible = false;
            }
            if (screenPanel != null) screenPanel.Visible = true;
            SetAirbusAocChromeVisible(true);
        }

        private void CduDisplayPanel_LskPressed(object sender, CduLskEventArgs e)
        {
            // Route through the single companion hub so on-screen and hardware LSKs share
            // exactly one path.
            Vns430Command command = e.RightSide
                ? (Vns430Command)((int)Vns430Command.DcduRightLsk1 + (e.Index - 1))
                : (Vns430Command)((int)Vns430Command.DcduLeftLsk1 + (e.Index - 1));
            HandleDcduCompanionCommand(command);
        }

        // Invoked from HandleDcduCompanionCommand's CDU arm.
        private void HandleCduLineSelect(bool rightSide, int index)
        {
            switch (cduPage)
            {
                case CduPageId.Menu:
                    HandleCduMenuLsk(rightSide, index);
                    break;
                case CduPageId.Dlk:
                    HandleCduDlkLsk(rightSide, index);
                    break;
                case CduPageId.Messages:
                    HandleCduMessagesLsk(rightSide, index);
                    break;
                case CduPageId.MessageDetail:
                    HandleCduMessageDetailLsk(rightSide, index);
                    break;
                case CduPageId.Atc:
                    HandleCduAtcLsk(rightSide, index);
                    break;
                case CduPageId.Aoc:
                    HandleCduAocLsk(rightSide, index);
                    break;
                case CduPageId.Request:
                    HandleCduRequestLsk(rightSide, index);
                    break;
            }

            RefreshCduDisplay();
        }

        // ---- Rendering -----------------------------------------------------

        private void RefreshCduDisplay()
        {
            if (cduDisplayPanel == null || cduDisplayPanel.IsDisposed)
            {
                return;
            }

            Vns430BackendSnapshot snapshot = GetVns430Snapshot();
            CduGrid grid = cduDisplayPanel.Grid;
            grid.Clear();

            switch (cduPage)
            {
                case CduPageId.Menu:
                    RenderCduMenu(grid, snapshot);
                    break;
                case CduPageId.Dlk:
                    RenderCduDlk(grid, snapshot);
                    break;
                case CduPageId.Messages:
                    RenderCduMessages(grid, snapshot);
                    break;
                case CduPageId.MessageDetail:
                    RenderCduMessageDetail(grid, snapshot);
                    break;
                case CduPageId.Atc:
                    RenderCduRequestMenu(grid, snapshot, "ATC REQUESTS", CduAtcMenuItems);
                    break;
                case CduPageId.Aoc:
                    RenderCduRequestMenu(grid, snapshot, "AOC / TELEX", CduAocMenuItems);
                    break;
                case CduPageId.Request:
                    RenderCduRequest(grid, snapshot);
                    break;
            }

            cduDisplayPanel.RefreshDisplay();
        }

        // Shared title row: page name centred, callsign at far left, link state at far right.
        private static void RenderCduHeader(CduGrid grid, string title, Vns430BackendSnapshot snapshot)
        {
            grid.WriteCentered(CduLayout.TitleRow, title, CduColor.White);
            if (!string.IsNullOrWhiteSpace(snapshot.Callsign))
            {
                grid.Write(CduLayout.TitleRow, 0, Truncate(snapshot.Callsign, 7), CduColor.Green, small: true);
            }
            grid.WriteRight(CduLayout.TitleRow, snapshot.Connected ? "CONN" : "OFFL",
                snapshot.Connected ? CduColor.Green : CduColor.Amber, small: true);
        }

        private static void RenderCduMenu(CduGrid grid, Vns430BackendSnapshot snapshot)
        {
            RenderCduHeader(grid, "EASYCPDLC CDU", snapshot);
            grid.WriteLeft(CduLayout.DataRow(1), "<DLK", CduColor.White);
            grid.WriteLeft(CduLayout.DataRow(2), "<ATC", CduColor.White);
            grid.WriteLeft(CduLayout.DataRow(3), "<AOC", CduColor.White);
            grid.WriteLeft(CduLayout.DataRow(4), "<MSG", CduColor.White);
            grid.WriteLeft(CduLayout.DataRow(5), "<SETUP", CduColor.White);

            // Right-hand hints for the pages not yet fully ported.
            grid.WriteRight(CduLayout.LabelRow(2), "STATUS/LOGON", CduColor.Cyan, small: true);
            grid.WriteRight(CduLayout.LabelRow(3), "REQUESTS", CduColor.Cyan, small: true);
            grid.WriteRight(CduLayout.LabelRow(4), "TELEX/WX/LOAD", CduColor.Cyan, small: true);
            grid.WriteRight(CduLayout.LabelRow(5), "INBOX", CduColor.Cyan, small: true);
        }

        private void RenderCduDlk(CduGrid grid, Vns430BackendSnapshot snapshot)
        {
            RenderCduHeader(grid, "DLK STATUS", snapshot);

            // Left column: actions on the LSKs.
            grid.WriteLeft(CduLayout.DataRow(1), snapshot.Connected ? "<DISCONNECT" : "<CONNECT",
                snapshot.Connected ? CduColor.Amber : CduColor.Green);
            grid.WriteLeft(CduLayout.DataRow(2), "<RELOAD FP", CduColor.White);
            grid.WriteLeft(CduLayout.DataRow(3), "<PRINT LAST", CduColor.White);
            grid.WriteLeft(CduLayout.DataRow(4), "<REPRINT", CduColor.White);
            grid.WriteLeft(CduLayout.DataRow(6), "<MENU", CduColor.White);

            // Right column: live status read-out (the old top-row info, now in the display).
            RenderCduRightStatus(grid, 1, "VATSIM", snapshot.Connected ? "CONNECTED" : "OFFLINE",
                snapshot.Connected ? CduColor.Green : CduColor.Amber);
            RenderCduRightStatus(grid, 2, "ATS UNIT", string.IsNullOrWhiteSpace(snapshot.CurrentAtcUnit) ? "----" : snapshot.CurrentAtcUnit, CduColor.Green);
            RenderCduRightStatus(grid, 3, "ROUTE", BuildRouteText(snapshot), CduColor.White);
            RenderCduRightStatus(grid, 4, "LOGON", string.IsNullOrWhiteSpace(snapshot.PendingLogon) ? "----" : snapshot.PendingLogon, CduColor.Cyan);
        }

        private static void RenderCduRightStatus(CduGrid grid, int lsk, string label, string value, CduColor valueColour)
        {
            grid.WriteRight(CduLayout.LabelRow(lsk), label, CduColor.Cyan, small: true);
            grid.WriteRight(CduLayout.DataRow(lsk), Truncate(value, CduGrid.HalfCols), valueColour);
        }

        private void RenderCduMessages(CduGrid grid, Vns430BackendSnapshot snapshot)
        {
            RenderCduHeader(grid, "MESSAGES", snapshot);

            cduVisibleInbox.Clear();
            List<Vns430MessageSnapshot> inbox = snapshot.Messages.Take(CduLayout.LskCount).ToList();
            for (int i = 0; i < inbox.Count; i++)
            {
                Vns430MessageSnapshot message = inbox[i];
                cduVisibleInbox.Add(message.Source);
                CduColor colour = message.Outbound ? CduColor.Cyan : (message.Unread ? CduColor.Amber : CduColor.White);
                string station = string.IsNullOrWhiteSpace(message.Station) ? message.Type : message.Station;
                string label = (message.Outbound ? ">" : "<") + Truncate(station + " " + message.Type, CduGrid.HalfCols - 1);
                grid.WriteLeft(CduLayout.DataRow(i + 1), label, colour);
            }

            if (snapshot.Messages.Count == 0)
            {
                grid.WriteCentered(CduLayout.DataRow(3), "NO MESSAGES", CduColor.Grey);
            }
            else if (snapshot.Messages.Count > CduLayout.LskCount)
            {
                grid.WriteRight(CduLayout.LabelRow(1),
                    "+" + (snapshot.Messages.Count - CduLayout.LskCount) + " MORE", CduColor.Grey, small: true);
            }

            grid.WriteRight(CduLayout.DataRow(6), "MENU>", CduColor.White);
        }

        private void RenderCduMessageDetail(CduGrid grid, Vns430BackendSnapshot snapshot)
        {
            Vns430MessageSnapshot message = FindCduSelected(snapshot);
            if (message == null)
            {
                cduPage = CduPageId.Messages;
                RenderCduMessages(grid, snapshot);
                return;
            }

            // Custom title (no callsign) so a long station name does not collide.
            string station = string.IsNullOrWhiteSpace(message.Station) ? message.Type : message.Station;
            grid.WriteCentered(CduLayout.TitleRow, Truncate(station, 18), CduColor.White);
            grid.WriteRight(CduLayout.TitleRow, message.Outbound ? "SENT" : "RCVD", CduColor.Cyan, small: true);

            // Body text wrapped across the upper rows (1..6), clear of the bottom LSKs.
            List<string> lines = WrapCduText(message.Text, CduGrid.Cols);
            for (int i = 0; i < lines.Count && i < 6; i++)
            {
                grid.WriteCentered(i + 1, lines[i], CduColor.Green, small: true);
            }

            // Bottom-left LSKs (4,5,6): available CPDLC replies.
            IReadOnlyList<string> responses = message.Responses ?? Array.Empty<string>();
            for (int i = 0; i < responses.Count && i < 3; i++)
            {
                grid.WriteLeft(CduLayout.DataRow(4 + i), "<" + responses[i], ReplyColour(responses[i]));
            }

            // Bottom-right LSKs (4,5,6): print actions + return.
            grid.WriteRight(CduLayout.DataRow(4), "PRINT>", CduColor.White);
            grid.WriteRight(CduLayout.DataRow(5), "REPRINT>", CduColor.White);
            grid.WriteRight(CduLayout.DataRow(6), "RETURN>", CduColor.White);
        }

        // ---- LSK handlers --------------------------------------------------

        private void HandleCduMenuLsk(bool rightSide, int index)
        {
            if (rightSide)
            {
                return;
            }

            switch (index)
            {
                case 1: cduPage = CduPageId.Dlk; break;
                case 2: cduPage = CduPageId.Atc; break;
                case 3: cduPage = CduPageId.Aoc; break;
                case 4: cduPage = CduPageId.Messages; break;
                // SETUP (5) grid page is the next porting step.
            }
        }

        private void HandleCduDlkLsk(bool rightSide, int index)
        {
            if (rightSide)
            {
                return;
            }

            switch (index)
            {
                case 1: Vns430ToggleVatsimConnection(); break;
                case 2: ReloadFlightPlanButton_Click(mainReloadFlightPlanButton, EventArgs.Empty); break;
                case 3: PrintButton_Click(refreshButtonVisual, EventArgs.Empty); break;
                case 4: ReprintButton_Click(boeingReprintButton, EventArgs.Empty); break;
                case 6: cduPage = CduPageId.Menu; break;
            }
        }

        private void HandleCduMessagesLsk(bool rightSide, int index)
        {
            if (rightSide)
            {
                if (index == 6)
                {
                    cduPage = CduPageId.Menu;
                }
                return;
            }

            int position = index - 1;
            if (position >= 0 && position < cduVisibleInbox.Count)
            {
                cduSelectedMessage = cduVisibleInbox[position];
                MarkMessageRead(cduSelectedMessage);
                cduPage = CduPageId.MessageDetail;
            }
        }

        private void HandleCduMessageDetailLsk(bool rightSide, int index)
        {
            Vns430MessageSnapshot message = FindCduSelected(GetVns430Snapshot());
            if (message == null)
            {
                cduPage = CduPageId.Messages;
                return;
            }

            if (!rightSide)
            {
                // Replies live on the bottom-left LSKs 4,5,6.
                IReadOnlyList<string> responses = message.Responses ?? Array.Empty<string>();
                int position = index - 4;
                if (position >= 0 && position < responses.Count && position < 3)
                {
                    Vns430Reply(message, responses[position]);
                }
                return;
            }

            switch (index)
            {
                case 4: PrintDatalinkMessage(message.Source); break;
                case 5: ReprintButton_Click(boeingReprintButton, EventArgs.Empty); break;
                case 6: cduPage = CduPageId.Messages; break;
            }
        }

        // ---- ATC / AOC request pages --------------------------------------

        private static readonly (string Label, Vns430WorkflowKind Kind)[] CduAtcMenuItems =
        {
            ("DIRECT TO", Vns430WorkflowKind.AtcDirect),
            ("LEVEL", Vns430WorkflowKind.AtcLevel),
            ("SPEED", Vns430WorkflowKind.AtcSpeed),
            ("WHEN CAN WE", Vns430WorkflowKind.AtcWhenCanWe),
            ("FREE TEXT", Vns430WorkflowKind.AtcFreeText)
        };

        private static readonly (string Label, Vns430WorkflowKind Kind)[] CduAocMenuItems =
        {
            ("TELEX", Vns430WorkflowKind.AocTelex),
            ("METAR", Vns430WorkflowKind.AocMetar),
            ("ATIS", Vns430WorkflowKind.AocAtis),
            ("PDC / PREDEP", Vns430WorkflowKind.AocPreDeparture),
            ("OCEANIC", Vns430WorkflowKind.AocOceanic)
        };

        private void RenderCduRequestMenu(CduGrid grid, Vns430BackendSnapshot snapshot, string title,
            (string Label, Vns430WorkflowKind Kind)[] items)
        {
            RenderCduHeader(grid, title, snapshot);
            for (int i = 0; i < items.Length && i < 5; i++)
            {
                grid.WriteLeft(CduLayout.DataRow(i + 1), "<" + items[i].Label, CduColor.White);
            }
            grid.WriteRight(CduLayout.DataRow(6), "MENU>", CduColor.White);
        }

        private void HandleCduAtcLsk(bool rightSide, int index) => HandleCduRequestMenuSelection(rightSide, index, CduAtcMenuItems);

        private void HandleCduAocLsk(bool rightSide, int index) => HandleCduRequestMenuSelection(rightSide, index, CduAocMenuItems);

        private void HandleCduRequestMenuSelection(bool rightSide, int index,
            (string Label, Vns430WorkflowKind Kind)[] items)
        {
            if (rightSide)
            {
                if (index == 6)
                {
                    cduPage = CduPageId.Menu;
                }
                return;
            }

            int position = index - 1;
            if (position >= 0 && position < items.Length && position < 5)
            {
                cduWorkflow = Vns430Workflow.Create(items[position].Kind, GetVns430Snapshot());
                cduScratchpad = string.Empty;
                cduStatusLine = string.Empty;
                cduPage = CduPageId.Request;
            }
        }

        // Request fields fill left LSK 2..5 then right LSK 2..5 (eight slots). LSK1's label
        // row is the title; LSK6 is RETURN / SEND with the scratchpad just above it.
        private static (bool RightSide, int Lsk) CduFieldSlot(int fieldIndex)
        {
            return fieldIndex < 4 ? (false, fieldIndex + 2) : (true, (fieldIndex - 4) + 2);
        }

        private void RenderCduRequest(CduGrid grid, Vns430BackendSnapshot snapshot)
        {
            if (cduWorkflow == null)
            {
                cduPage = CduPageId.Menu;
                RenderCduMenu(grid, snapshot);
                return;
            }

            grid.WriteCentered(CduLayout.TitleRow, Truncate(cduWorkflow.Title, 24), CduColor.White);

            for (int i = 0; i < cduWorkflow.Fields.Count && i < 8; i++)
            {
                Vns430EditField field = cduWorkflow.Fields[i];
                (bool right, int lsk) = CduFieldSlot(i);
                bool empty = string.IsNullOrWhiteSpace(field.CleanValue);
                string value = empty ? (field.IsOption ? "----" : "[   ]") : field.CleanValue;
                CduColor colour = empty ? CduColor.Grey : CduColor.Green;

                if (right)
                {
                    grid.WriteRight(CduLayout.LabelRow(lsk), field.Label, CduColor.Cyan, small: true);
                    grid.WriteRight(CduLayout.DataRow(lsk), Truncate(value, CduGrid.HalfCols - 1) + ">", colour);
                }
                else
                {
                    grid.WriteLeft(CduLayout.LabelRow(lsk), field.Label, CduColor.Cyan, small: true);
                    grid.WriteLeft(CduLayout.DataRow(lsk), "<" + Truncate(value, CduGrid.HalfCols - 1), colour);
                }
            }

            // Scratchpad (or the last status message) sits just above the RETURN/SEND row.
            if (!string.IsNullOrWhiteSpace(cduStatusLine))
            {
                grid.WriteCentered(CduLayout.LabelRow(6), Truncate(cduStatusLine, CduGrid.Cols), CduColor.Amber, small: true);
            }
            else
            {
                grid.WriteCentered(CduLayout.LabelRow(6), "[" + Truncate(cduScratchpad, CduGrid.Cols - 2) + "]", CduColor.White);
            }

            grid.WriteLeft(CduLayout.DataRow(6), "<RETURN", CduColor.White);
            grid.WriteRight(CduLayout.DataRow(6), cduRequestSending ? "SENDING" : "SEND>",
                cduRequestSending ? CduColor.Grey : CduColor.Green);
        }

        private void HandleCduRequestLsk(bool rightSide, int index)
        {
            if (cduWorkflow == null)
            {
                cduPage = CduPageId.Menu;
                return;
            }

            if (index == 6)
            {
                if (rightSide)
                {
                    CduSendCurrentWorkflow();
                }
                else
                {
                    cduPage = cduWorkflow.Kind.ToString().StartsWith("Atc", StringComparison.Ordinal)
                        ? CduPageId.Atc
                        : CduPageId.Aoc;
                    cduWorkflow = null;
                }
                return;
            }

            if (index == 1)
            {
                return; // LSK1 carries no field on request pages
            }

            int fieldIndex = rightSide ? 4 + (index - 2) : index - 2;
            if (fieldIndex < 0 || fieldIndex >= cduWorkflow.Fields.Count)
            {
                return;
            }

            Vns430EditField field = cduWorkflow.Fields[fieldIndex];
            cduStatusLine = string.Empty;
            if (field.IsOption)
            {
                field.Step(0, 1); // cycle to the next option
            }
            else if (!string.IsNullOrEmpty(cduScratchpad))
            {
                field.Value = Truncate(cduScratchpad, field.MaxLength);
                cduScratchpad = string.Empty;
            }
            else
            {
                field.Value = string.Empty; // an empty scratchpad clears the field
            }
        }

        private async void CduSendCurrentWorkflow()
        {
            if (cduWorkflow == null || cduRequestSending)
            {
                return;
            }

            cduRequestSending = true;
            cduStatusLine = "SENDING...";
            RefreshCduDisplay();

            Vns430OperationResult result = await Vns430SendWorkflowAsync(cduWorkflow, GetVns430Snapshot());

            cduRequestSending = false;
            cduStatusLine = result.Status;
            if (result.Success)
            {
                cduWorkflow = null;
                cduPage = CduPageId.Messages;
            }
            RefreshCduDisplay();
        }

        private void CduScratchpadType(char c)
        {
            if (cduPage == CduPageId.Request && cduScratchpad.Length < CduGrid.Cols - 2)
            {
                cduScratchpad += c;
                cduStatusLine = string.Empty;
                RefreshCduDisplay();
            }
        }

        private void CduScratchpadBackspace()
        {
            if (cduPage == CduPageId.Request && cduScratchpad.Length > 0)
            {
                cduScratchpad = cduScratchpad.Substring(0, cduScratchpad.Length - 1);
                RefreshCduDisplay();
            }
        }

        private void CduScratchpadClearAll()
        {
            if (cduPage == CduPageId.Request)
            {
                cduScratchpad = string.Empty;
                cduStatusLine = string.Empty;
                RefreshCduDisplay();
            }
        }

        // ---- Helpers -------------------------------------------------------

        private Vns430MessageSnapshot FindCduSelected(Vns430BackendSnapshot snapshot)
        {
            if (cduSelectedMessage == null)
            {
                return null;
            }
            return snapshot.Messages.FirstOrDefault(m => ReferenceEquals(m.Source, cduSelectedMessage));
        }

        private static CduColor ReplyColour(string response)
        {
            return (response ?? string.Empty).ToUpperInvariant() switch
            {
                "WILCO" or "AFFIRMATIVE" or "ROGER" => CduColor.Green,
                "STANDBY" => CduColor.Amber,
                _ => CduColor.White
            };
        }

        private static string BuildRouteText(Vns430BackendSnapshot snapshot)
        {
            string dep = string.IsNullOrWhiteSpace(snapshot.Departure) ? "----" : snapshot.Departure;
            string arr = string.IsNullOrWhiteSpace(snapshot.Arrival) ? "----" : snapshot.Arrival;
            return dep + "-" + arr;
        }

        private static string Truncate(string value, int width)
        {
            value ??= string.Empty;
            return value.Length <= width ? value : value.Substring(0, width);
        }

        private static List<string> WrapCduText(string text, int width)
        {
            List<string> output = new();
            foreach (string rawLine in (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string remaining = rawLine.Trim();
                if (remaining.Length == 0)
                {
                    output.Add(string.Empty);
                    continue;
                }

                while (remaining.Length > width)
                {
                    int splitAt = remaining.LastIndexOf(' ', Math.Min(width - 1, remaining.Length - 1));
                    if (splitAt < 1)
                    {
                        splitAt = width;
                        output.Add(remaining.Substring(0, splitAt));
                        remaining = remaining.Substring(splitAt);
                        continue;
                    }
                    output.Add(remaining.Substring(0, splitAt).TrimEnd());
                    remaining = remaining.Substring(splitAt + 1);
                }
                output.Add(remaining);
            }
            return output;
        }
    }
}
