using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyCPDLC.GNS430
{
    internal sealed class Gns430Form : Form
    {
        private const int LogicalWidth = 960;
        private const int LogicalHeight = 407;
        private const int WmAppSimConnect = 0x8430;

        private static readonly Color BezelDark = Color.FromArgb(105, 108, 103);
        private static readonly Color BezelLight = Color.FromArgb(202, 202, 190);
        private static readonly Color ScreenBlue = Color.FromArgb(0, 61, 128);
        private static readonly Color ScreenBlueTop = Color.FromArgb(0, 78, 148);
        private static readonly Color ScreenWhite = Color.FromArgb(235, 244, 229);
        private static readonly Color ScreenGreen = Color.FromArgb(88, 255, 118);
        private static readonly Color ScreenCyan = Color.FromArgb(90, 226, 255);
        private static readonly Color ScreenAmber = Color.FromArgb(255, 232, 0);
        private static readonly Color ScreenMagenta = Color.FromArgb(255, 82, 215);

        private static readonly RectangleF ScreenBounds = new(235, 55, 518, 277);
        private static readonly RectangleF MainScreenBounds = ScreenBounds;

        private readonly MainForm backend;
        private readonly Gns430Preferences preferences;
        private readonly Gns430CompanionInput companionInput = new();
        private readonly Timer refreshTimer = new() { Interval = 100 };
        private readonly ToolTip toolTip = new();
        private readonly Gns430PanelArtwork artwork = new();
        private readonly List<PanelButton> panelButtons;
        private readonly FontFamily displayFontFamily;

        private Gns430BackendSnapshot snapshot = new();
        private Gns430Page page = Gns430Page.Status;
        private Gns430Page pageBeforeOverlay = Gns430Page.Status;
        private Gns430PageGroup pageGroup = Gns430PageGroup.Nav;
        private Gns430PageGroup groupBeforeOverlay = Gns430PageGroup.Nav;
        private int selectedIndex;
        private int detailScrollLine;
        private int responseIndex;
        private int refreshTick;
        private int zoomLevel = 1;
        private bool cursorActive;
        private string logonCode = "____";
        private int logonCharacter;
        private string transientStatus = string.Empty;
        private DateTime transientStatusUntilUtc = DateTime.MinValue;
        private string activeArtworkControl = string.Empty;
        private string activeArtworkState = string.Empty;
        private DateTime activeArtworkUntilUtc = DateTime.MinValue;
        private PanelButton pressedPanelButton;
        private bool pressedCursor;
        private bool pressedScreen;
        private Gns430Workflow workflow;
        private int workflowCharacter;
        private Gns430LoadControlSession loadSession;
        private bool operationBusy;
        private string operationStatus = string.Empty;

        private static readonly string[] AtcMenuItems =
        {
            "DIRECT TO", "LEVEL", "SPEED", "WHEN CAN WE", "FREE TEXT"
        };

        private static readonly string[] AocMenuItems =
        {
            "AOC TELEX", "METAR", "ATIS", "PREDEP CLEARANCE", "OCEANIC CLEARANCE", "LOAD CONTROL"
        };

        private sealed class PanelButton
        {
            internal RectangleF Bounds { get; init; }
            internal string Label { get; init; } = string.Empty;
            internal string ArtworkControl { get; init; } = string.Empty;
            internal Gns430Command Command { get; init; }
        }

        internal Gns430Form(MainForm backend)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            preferences = Gns430Preferences.Load();

            Text = "EasyCPDLC - GNS 430 Datalink";
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(730, 355);
            ClientSize = new Size(LogicalWidth, LogicalHeight);
            BackColor = BezelDark;
            DoubleBuffered = true;
            TopMost = true;
            Icon = backend.Icon;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MaximizeBox = false;

            Rectangle saved = new(preferences.Left, preferences.Top, preferences.Width, preferences.Height);
            if (preferences.Left >= 0 && preferences.Top >= 0 && Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(saved)))
            {
                Bounds = saved;
            }
            else
            {
                Rectangle work = Screen.FromControl(backend).WorkingArea;
                Location = new Point(
                    Math.Max(work.Left, work.Right - Width - 24),
                    Math.Max(work.Top, work.Bottom - Height - 24));
            }

            displayFontFamily = Gns430FontLoader.Family;
            panelButtons = CreatePanelButtons();

            companionInput.CommandReceived += command => BeginInvoke(new Action(() => HandleCompanionCommand(command)));
            refreshTimer.Tick += RefreshTimerTick;
            refreshTimer.Start();

            Paint += PaintPanel;
            MouseDown += PanelMouseDown;
            MouseUp += PanelMouseUp;
            MouseWheel += PanelMouseWheel;
            MouseCaptureChanged += PanelMouseCaptureChanged;
            FormClosing += PanelFormClosing;
            VisibleChanged += (_, __) =>
            {
                if (Visible)
                {
                    RefreshSnapshot();
                    Invalidate();
                }
            };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (preferences.CompanionModuleEnabled)
            {
                companionInput.TryEnable(Handle, WmAppSimConnect, out _);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer.Dispose();
                companionInput.Dispose();
                toolTip.Dispose();
                artwork.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmAppSimConnect)
            {
                companionInput.ReceiveWindowMessage();
                return;
            }

            base.WndProc(ref m);
        }

        private void RefreshTimerTick(object sender, EventArgs e)
        {
            if (preferences.CompanionModuleEnabled && !companionInput.Enabled && refreshTick % 50 == 0)
            {
                companionInput.TryEnable(Handle, WmAppSimConnect, out _);
            }

            companionInput.UpdateStatus(snapshot, page, cursorActive, preferences.DcduCompanionMode);
            refreshTick += 1;
            if (refreshTick % 3 == 0)
            {
                RefreshSnapshot();
            }

            Invalidate();
        }

        private void RefreshSnapshot()
        {
            snapshot = backend.GetGns430Snapshot();
            if (!ShouldPreserveMessageSelection(page))
            {
                return;
            }

            if (snapshot.Messages.Count == 0)
            {
                selectedIndex = 0;
            }
            else
            {
                selectedIndex = Math.Clamp(selectedIndex, 0, snapshot.Messages.Count - 1);
            }
        }

        internal void ExecuteCommand(Gns430Command command)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ExecuteCommand(command)));
                return;
            }

            SetArtworkFeedback(command);

            switch (command)
            {
                case Gns430Command.LargeRightDecrease:
                    RotateLarge(-1);
                    break;
                case Gns430Command.LargeRightIncrease:
                    RotateLarge(1);
                    break;
                case Gns430Command.SmallRightDecrease:
                    RotateSmall(-1);
                    break;
                case Gns430Command.SmallRightIncrease:
                    RotateSmall(1);
                    break;
                case Gns430Command.CursorPush:
                case Gns430Command.Obs:
                    ToggleCursor();
                    break;
                case Gns430Command.Enter:
                    ActivateSelection();
                    break;
                case Gns430Command.Clear:
                    ClearOrBack();
                    break;
                case Gns430Command.Menu:
                    ToggleMenu();
                    break;
                case Gns430Command.Message:
                    OpenMessages(true);
                    break;
                case Gns430Command.Nearest:
                    OpenMessages(false);
                    break;
                case Gns430Command.FlightPlan:
                    SetPage(Gns430Page.AtcMenu, true, Gns430PageGroup.Wpt);
                    break;
                case Gns430Command.Procedure:
                    SetPage(Gns430Page.AocMenu, true, Gns430PageGroup.Aux);
                    break;
                case Gns430Command.DirectTo:
                    SetPage(Gns430Page.Logon, true, Gns430PageGroup.Wpt);
                    break;
                case Gns430Command.Cdi:
                    backend.Gns430ToggleVatsimConnection();
                    SetTransient(snapshot.Connected ? "DISCONNECTING" : "CONNECTING");
                    break;
                case Gns430Command.RangeIn:
                    zoomLevel = Math.Min(2, zoomLevel + 1);
                    break;
                case Gns430Command.RangeOut:
                    zoomLevel = Math.Max(0, zoomLevel - 1);
                    break;
                case Gns430Command.Power:
                    if (Visible)
                    {
                        Hide();
                    }
                    else
                    {
                        Show();
                        BringToFront();
                    }
                    break;
            }

            Invalidate();
        }

        private void RotateLarge(int direction)
        {
            if ((page == Gns430Page.AtcRequest || page == Gns430Page.AocRequest) && workflow != null)
            {
                MoveWorkflowCursor(direction);
                return;
            }

            if (page == Gns430Page.LoadControl && loadSession != null && !operationBusy)
            {
                selectedIndex = Wrap(selectedIndex + direction, loadSession.FieldCount);
                return;
            }

            if (page == Gns430Page.Logon && cursorActive)
            {
                logonCharacter = Wrap(logonCharacter + direction, 4);
                return;
            }

            if (page == Gns430Page.MessageDetail)
            {
                detailScrollLine = Math.Max(0, detailScrollLine + direction);
                return;
            }

            if (page == Gns430Page.Messages)
            {
                if (cursorActive && snapshot.Messages.Count > 0)
                {
                    selectedIndex = Wrap(selectedIndex + direction, snapshot.Messages.Count);
                }
                else if (!cursorActive)
                {
                    CyclePageGroup(direction);
                }
                return;
            }

            if (page == Gns430Page.Menu)
            {
                selectedIndex = Wrap(selectedIndex + direction, MenuItems().Count);
                return;
            }

            if (page == Gns430Page.AtcMenu || page == Gns430Page.AocMenu)
            {
                int count = page == Gns430Page.AtcMenu ? AtcMenuItems.Length : AocMenuItems.Length;
                selectedIndex = Wrap(selectedIndex + direction, count);
                return;
            }

            if (page == Gns430Page.Help)
            {
                if (cursorActive)
                {
                    detailScrollLine = Math.Max(0, detailScrollLine + direction);
                }
                else
                {
                    CyclePageGroup(direction);
                }
                return;
            }

            if (cursorActive)
            {
                selectedIndex = Wrap(selectedIndex + direction, 4);
            }
            else
            {
                CyclePageGroup(direction);
            }
        }

        private void RotateSmall(int direction)
        {
            if ((page == Gns430Page.AtcRequest || page == Gns430Page.AocRequest) && workflow != null)
            {
                workflow.Fields[Math.Clamp(selectedIndex, 0, workflow.Fields.Count - 1)].Step(workflowCharacter, direction);
                return;
            }

            if (page == Gns430Page.LoadControl && loadSession != null && !operationBusy)
            {
                StepLoadControlField(direction);
                return;
            }

            if (page == Gns430Page.Logon && cursorActive)
            {
                char[] characters = "_ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
                char current = logonCode[logonCharacter];
                int characterIndex = Array.IndexOf(characters, current);
                characterIndex = Wrap(characterIndex + direction, characters.Length);
                char[] code = logonCode.ToCharArray();
                code[logonCharacter] = characters[characterIndex];
                logonCode = new string(code);
                return;
            }

            if (page == Gns430Page.MessageDetail)
            {
                Gns430MessageSnapshot message = SelectedMessage();
                if (message?.Responses.Count > 0)
                {
                    responseIndex = Wrap(responseIndex + direction, message.Responses.Count);
                }
                return;
            }

            if (cursorActive)
            {
                return;
            }

            CyclePageWithinGroup(direction);
        }

        private void ToggleCursor()
        {
            if (page == Gns430Page.Menu)
            {
                cursorActive = true;
                return;
            }

            cursorActive = !cursorActive;
        }

        private async void ActivateSelection()
        {
            switch (page)
            {
                case Gns430Page.Status:
                    if (!cursorActive)
                    {
                        cursorActive = true;
                        return;
                    }

                    switch (selectedIndex)
                    {
                        case 0:
                            SetPage(Gns430Page.Messages, true, Gns430PageGroup.Nrst);
                            break;
                        case 1:
                            SetPage(Gns430Page.Logon, true, Gns430PageGroup.Wpt);
                            break;
                        case 2:
                            backend.Gns430ToggleVatsimConnection();
                            SetTransient(snapshot.Connected ? "DISCONNECTING" : "CONNECTING");
                            break;
                        case 3:
                            SetPage(Gns430Page.AtcMenu, true, Gns430PageGroup.Wpt);
                            break;
                    }
                    break;

                case Gns430Page.Messages:
                    if (SelectedMessage() != null)
                    {
                        SetPage(Gns430Page.MessageDetail, true);
                        backend.Gns430MarkRead(SelectedMessage());
                    }
                    break;

                case Gns430Page.MessageDetail:
                    Gns430MessageSnapshot message = SelectedMessage();
                    if (message?.Responses.Count > 0)
                    {
                        string response = message.Responses[Math.Clamp(responseIndex, 0, message.Responses.Count - 1)];
                        backend.Gns430Reply(message, response);
                        SetTransient(response + " SENDING");
                    }
                    else if (message != null)
                    {
                        backend.Gns430MarkRead(message);
                        SetTransient("MESSAGE READ");
                    }
                    break;

                case Gns430Page.Logon:
                    string station = logonCode.Replace("_", string.Empty).Trim();
                    if (station.Length < 3)
                    {
                        cursorActive = true;
                        SetTransient("ENTER 3-4 CHAR CODE");
                        return;
                    }

                    await backend.Gns430RequestLogonAsync(station);
                    cursorActive = false;
                    SetTransient("LOGON SENT " + station);
                    break;

                case Gns430Page.AtcMenu:
                    BeginWorkflow((Gns430WorkflowKind)(selectedIndex + (int)Gns430WorkflowKind.AtcDirect));
                    break;

                case Gns430Page.AocMenu:
                    if (selectedIndex == AocMenuItems.Length - 1)
                    {
                        await OpenLoadControlAsync();
                    }
                    else
                    {
                        BeginWorkflow((Gns430WorkflowKind)(selectedIndex + (int)Gns430WorkflowKind.AocTelex));
                    }
                    break;

                case Gns430Page.AtcRequest:
                case Gns430Page.AocRequest:
                    string validation = workflow?.ValidationError() ?? "NO REQUEST";
                    if (!string.IsNullOrWhiteSpace(validation))
                    {
                        SetTransient(validation);
                        return;
                    }
                    SetPage(page == Gns430Page.AtcRequest ? Gns430Page.RequestReview : Gns430Page.AocReview, false);
                    break;

                case Gns430Page.RequestReview:
                case Gns430Page.AocReview:
                    await SendWorkflowAsync();
                    break;

                case Gns430Page.LoadControl:
                    if (loadSession == null)
                    {
                        await OpenLoadControlAsync();
                    }
                    else
                    {
                        SetPage(Gns430Page.LoadReview, false);
                    }
                    break;

                case Gns430Page.LoadReview:
                    await GenerateLoadsheetAsync();
                    break;

                case Gns430Page.Menu:
                    ActivateMenuItem(selectedIndex);
                    break;

                case Gns430Page.Help:
                    SetPage(pageBeforeOverlay, false);
                    break;
            }
        }

        private void ActivateMenuItem(int index)
        {
            switch (index)
            {
                case 0:
                    backend.Gns430ToggleVatsimConnection();
                    SetTransient(snapshot.Connected ? "DISCONNECTING" : "CONNECTING");
                    SetPage(Gns430Page.Status, false, Gns430PageGroup.Nav);
                    break;
                case 1:
                    SetPage(Gns430Page.AtcMenu, true, Gns430PageGroup.Wpt);
                    break;
                case 2:
                    SetPage(Gns430Page.AocMenu, true, Gns430PageGroup.Aux);
                    break;
                case 3:
                    backend.Gns430OpenSettings();
                    SetTransient("SETTINGS OPENED");
                    break;
                case 4:
                    ToggleCompanionModule();
                    break;
                case 5:
                    pageBeforeOverlay = Gns430Page.Menu;
                    SetPage(Gns430Page.Help, false);
                    break;
            }
        }

        private void ToggleCompanionModule()
        {
            if (companionInput.Enabled)
            {
                companionInput.Disable();
                preferences.CompanionModuleEnabled = false;
                preferences.DcduCompanionMode = false;
                preferences.Save(Bounds);
                SetTransient("MSFS MODULE OFF");
                return;
            }

            if (companionInput.TryEnable(Handle, WmAppSimConnect, out string error))
            {
                preferences.CompanionModuleEnabled = true;
                preferences.Save(Bounds);
                SetTransient("WAITING FOR MODULE");
            }
            else
            {
                preferences.CompanionModuleEnabled = false;
                preferences.DcduCompanionMode = false;
                preferences.Save(Bounds);
                MessageBox.Show(this, error, "Companion module unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetTransient("MSFS MODULE OFFLINE");
            }
        }

        private void ClearOrBack()
        {
            if ((page == Gns430Page.AtcRequest || page == Gns430Page.AocRequest) && workflow != null && cursorActive)
            {
                Gns430EditField field = workflow.Fields[Math.Clamp(selectedIndex, 0, workflow.Fields.Count - 1)];
                if (!string.IsNullOrWhiteSpace(field.CleanValue) && !field.IsOption)
                {
                    field.ClearCharacter(workflowCharacter);
                    return;
                }
                SetPage(page == Gns430Page.AtcRequest ? Gns430Page.AtcMenu : Gns430Page.AocMenu, true);
                return;
            }

            if (page == Gns430Page.RequestReview)
            {
                SetPage(Gns430Page.AtcRequest, true);
                return;
            }
            if (page == Gns430Page.AocReview)
            {
                SetPage(Gns430Page.AocRequest, true);
                return;
            }
            if (page == Gns430Page.LoadReview)
            {
                SetPage(Gns430Page.LoadControl, true);
                return;
            }
            if (page == Gns430Page.AtcMenu || page == Gns430Page.AocMenu || page == Gns430Page.LoadControl)
            {
                SetPage(Gns430Page.Status, false, Gns430PageGroup.Nav);
                return;
            }

            if (page == Gns430Page.Logon && cursorActive)
            {
                char[] code = logonCode.ToCharArray();
                if (code[logonCharacter] != '_')
                {
                    code[logonCharacter] = '_';
                    logonCode = new string(code);
                    return;
                }

                cursorActive = false;
                return;
            }

            if (page == Gns430Page.MessageDetail)
            {
                SetPage(Gns430Page.Messages, true);
            }
            else if (page == Gns430Page.Menu || page == Gns430Page.Help)
            {
                pageGroup = groupBeforeOverlay;
                SetPage(pageBeforeOverlay, false);
            }
            else
            {
                SetPage(Gns430Page.Status, false, Gns430PageGroup.Nav);
            }
        }

        private void ToggleMenu()
        {
            if (page == Gns430Page.Menu)
            {
                pageGroup = groupBeforeOverlay;
                SetPage(pageBeforeOverlay, false);
                return;
            }

            pageBeforeOverlay = page;
            groupBeforeOverlay = pageGroup;
            SetPage(Gns430Page.Menu, true);
        }

        private void CyclePageGroup(int direction)
        {
            pageGroup = (Gns430PageGroup)Wrap((int)pageGroup + direction, 4);
            SetPage(PagesForGroup(pageGroup)[0], false);
        }

        private void CyclePageWithinGroup(int direction)
        {
            Gns430Page[] pages = PagesForGroup(pageGroup);
            int current = Array.IndexOf(pages, page);
            if (current < 0)
            {
                current = 0;
            }
            SetPage(pages[Wrap(current + direction, pages.Length)], false);
        }

        internal static Gns430Page[] PagesForGroup(Gns430PageGroup group)
        {
            return group switch
            {
                Gns430PageGroup.Nav => new[] { Gns430Page.Status, Gns430Page.Messages },
                Gns430PageGroup.Wpt => new[] { Gns430Page.Logon, Gns430Page.AtcMenu },
                Gns430PageGroup.Aux => new[] { Gns430Page.AocMenu, Gns430Page.LoadControl, Gns430Page.Help },
                Gns430PageGroup.Nrst => new[] { Gns430Page.Messages },
                _ => new[] { Gns430Page.Status }
            };
        }

        private void SetPage(Gns430Page newPage, bool activateCursor, Gns430PageGroup? group = null)
        {
            if (group.HasValue)
            {
                pageGroup = group.Value;
            }
            page = newPage;
            cursorActive = activateCursor;
            detailScrollLine = 0;
            responseIndex = 0;
            selectedIndex = ShouldPreserveMessageSelection(newPage)
                ? Math.Clamp(selectedIndex, 0, Math.Max(0, snapshot.Messages.Count - 1))
                : 0;
            if (newPage != Gns430Page.AtcRequest && newPage != Gns430Page.AocRequest)
            {
                workflowCharacter = 0;
            }
        }

        private Gns430MessageSnapshot SelectedMessage()
        {
            return snapshot.Messages.Count == 0
                ? null
                : snapshot.Messages[Math.Clamp(selectedIndex, 0, snapshot.Messages.Count - 1)];
        }

        private List<string> MenuItems()
        {
            return new List<string>
            {
                snapshot.Connected ? "DISCONNECT VATSIM" : "CONNECT VATSIM",
                "ATC REQUEST MENU",
                "AOC / TELEX MENU",
                "EASYCPDLC SETTINGS",
                "MSFS MODULE: " + companionInput.Status,
                "INPUT HELP"
            };
        }

        private void OpenMessages(bool prioritizeUnread)
        {
            int unread = prioritizeUnread
                ? snapshot.Messages.ToList().FindIndex(message => message.Unread && !message.Outbound)
                : -1;
            if (unread >= 0)
            {
                selectedIndex = unread;
                SetPage(Gns430Page.MessageDetail, true, Gns430PageGroup.Nrst);
                backend.Gns430MarkRead(SelectedMessage());
                return;
            }

            SetPage(Gns430Page.Messages, true, Gns430PageGroup.Nrst);
        }

        private void BeginWorkflow(Gns430WorkflowKind kind)
        {
            workflow = Gns430Workflow.Create(kind, snapshot);
            workflowCharacter = 0;
            operationStatus = string.Empty;
            bool atc = kind >= Gns430WorkflowKind.AtcDirect && kind <= Gns430WorkflowKind.AtcFreeText;
            SetPage(atc ? Gns430Page.AtcRequest : Gns430Page.AocRequest, true,
                atc ? Gns430PageGroup.Wpt : Gns430PageGroup.Aux);
        }

        private void MoveWorkflowCursor(int direction)
        {
            if (workflow?.Fields.Count > 0)
            {
                Gns430EditField field = workflow.Fields[Math.Clamp(selectedIndex, 0, workflow.Fields.Count - 1)];
                if (field.IsOption)
                {
                    selectedIndex = Wrap(selectedIndex + direction, workflow.Fields.Count);
                    workflowCharacter = direction < 0
                        ? Math.Max(0, workflow.Fields[selectedIndex].MaxLength - 1)
                        : 0;
                    return;
                }

                int next = workflowCharacter + direction;
                if (next >= 0 && next < field.MaxLength)
                {
                    workflowCharacter = next;
                    return;
                }

                selectedIndex = Wrap(selectedIndex + direction, workflow.Fields.Count);
                Gns430EditField nextField = workflow.Fields[selectedIndex];
                workflowCharacter = nextField.IsOption || direction > 0 ? 0 : Math.Max(0, nextField.MaxLength - 1);
            }
        }

        private void StepLoadControlField(int direction)
        {
            if (loadSession == null)
            {
                return;
            }

            if (selectedIndex == 0)
            {
                loadSession.AircraftIndex = Wrap(loadSession.AircraftIndex + direction, loadSession.Reference.Aircraft.Count);
                loadSession.CabinIndex = 0;
                loadSession.RebuildPassengerSplit();
            }
            else if (selectedIndex == 1)
            {
                loadSession.CabinIndex = Wrap(loadSession.CabinIndex + direction, loadSession.Aircraft.CabinConfigurations.Count);
                loadSession.RebuildPassengerSplit();
            }
            else if (selectedIndex == 2)
            {
                loadSession.FormatIndex = Wrap(loadSession.FormatIndex + direction, loadSession.Reference.Formats.Count);
            }
            else
            {
                int passengerIndex = selectedIndex - 3;
                if (passengerIndex >= 0 && passengerIndex < loadSession.PassengerSplit.Count)
                {
                    PassengerClassAllocation allocation = loadSession.PassengerSplit[passengerIndex];
                    allocation.Passengers = Math.Clamp(allocation.Passengers + direction, 0, 999);
                }
            }
        }

        private async Task OpenLoadControlAsync()
        {
            SetPage(Gns430Page.LoadControl, true, Gns430PageGroup.Aux);
            operationBusy = true;
            operationStatus = "LOADING SIMBRIEF / ELOAD";
            try
            {
                loadSession = await backend.Gns430PrepareLoadControlAsync();
                operationStatus = "REVIEW LOAD DATA";
            }
            catch (Exception ex)
            {
                loadSession = null;
                operationStatus = CollapseWhitespace(ex.Message).ToUpperInvariant();
            }
            finally
            {
                operationBusy = false;
            }
        }

        private async Task SendWorkflowAsync()
        {
            if (operationBusy || workflow == null)
            {
                return;
            }

            operationBusy = true;
            operationStatus = "SENDING REQUEST";
            Gns430OperationResult result = await backend.Gns430SendWorkflowAsync(workflow, snapshot);
            operationBusy = false;
            operationStatus = result.Status;
            SetTransient(result.Status);
            if (result.Success)
            {
                RefreshSnapshot();
                selectedIndex = 0;
                SetPage(Gns430Page.Messages, true, Gns430PageGroup.Nav);
            }
        }

        private async Task GenerateLoadsheetAsync()
        {
            if (operationBusy || loadSession == null)
            {
                return;
            }

            operationBusy = true;
            operationStatus = "GENERATING LOADSHEET";
            Gns430OperationResult result = await backend.Gns430GenerateLoadsheetAsync(loadSession);
            operationBusy = false;
            operationStatus = result.Status;
            SetTransient(result.Status);
            if (result.Success)
            {
                RefreshSnapshot();
                int received = snapshot.Messages.ToList().FindIndex(message =>
                    message.Unread && string.Equals(message.Type, "LOADSHEET", StringComparison.OrdinalIgnoreCase));
                selectedIndex = Math.Max(0, received);
                SetPage(received >= 0 ? Gns430Page.MessageDetail : Gns430Page.Messages, true, Gns430PageGroup.Nrst);
                if (received >= 0)
                {
                    backend.Gns430MarkRead(SelectedMessage());
                }
            }
        }

        private void SetTransient(string text)
        {
            transientStatus = (text ?? string.Empty).Trim().ToUpperInvariant();
            transientStatusUntilUtc = DateTime.UtcNow.AddSeconds(3);
        }

        private void SetArtworkFeedback(Gns430Command command)
        {
            (string control, string state) = command switch
            {
                Gns430Command.LargeRightDecrease => ("right_encoder", "large-ccw"),
                Gns430Command.LargeRightIncrease => ("right_encoder", "large-cw"),
                Gns430Command.SmallRightDecrease => ("right_encoder", "small-ccw"),
                Gns430Command.SmallRightIncrease => ("right_encoder", "small-cw"),
                Gns430Command.CursorPush => ("right_encoder", "pushed"),
                Gns430Command.Enter => ("enter", "pressed"),
                Gns430Command.Clear => ("clear", "pressed"),
                Gns430Command.Menu => ("menu", "pressed"),
                Gns430Command.Message => ("msg", "pressed"),
                Gns430Command.FlightPlan => ("fpl", "pressed"),
                Gns430Command.Procedure => ("proc", "pressed"),
                Gns430Command.DirectTo => ("direct_to", "pressed"),
                Gns430Command.Cdi => ("cdi", "pressed"),
                Gns430Command.Obs => ("obs", "pressed"),
                Gns430Command.RangeIn => ("range", "increase-pressed"),
                Gns430Command.RangeOut => ("range", "decrease-pressed"),
                Gns430Command.Power => ("left_small_top", "cw"),
                _ => (string.Empty, string.Empty)
            };
            SetArtworkFeedback(control, state);
        }

        private void SetArtworkFeedback(string control, string state, bool held = false)
        {
            activeArtworkControl = control ?? string.Empty;
            activeArtworkState = state ?? string.Empty;
            activeArtworkUntilUtc = string.IsNullOrWhiteSpace(activeArtworkControl)
                ? DateTime.MinValue
                : held ? DateTime.MaxValue : DateTime.UtcNow.AddMilliseconds(220);
        }

        private void ClearArtworkFeedback()
        {
            activeArtworkControl = string.Empty;
            activeArtworkState = string.Empty;
            activeArtworkUntilUtc = DateTime.MinValue;
        }

        private void PaintPanel(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float scaleX = ClientSize.Width / (float)LogicalWidth;
            float scaleY = ClientSize.Height / (float)LogicalHeight;
            e.Graphics.ScaleTransform(scaleX, scaleY);

            DrawPanelSurface(e.Graphics);
        }

        private void DrawPanelSurface(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(artwork.PanelBackground, new RectangleF(0, 0, LogicalWidth, LogicalHeight));
            DrawScreen(graphics);
            if (DateTime.UtcNow < activeArtworkUntilUtc)
            {
                artwork.DrawState(graphics, activeArtworkControl, activeArtworkState);
            }
        }

        private void DrawBranding(Graphics graphics)
        {
            using Font brand = new(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold, GraphicsUnit.Point);
            using Font model = new(FontFamily.GenericSansSerif, 7.2f, FontStyle.Regular, GraphicsUnit.Point);
            using Brush ink = new SolidBrush(Color.FromArgb(22, 24, 22));
            graphics.DrawString("GARMIN", brand, ink, 28, 22);
            graphics.DrawString("GNS 430", model, ink, 882, 22);
            graphics.DrawString("COM", model, ink, 136, 220);
            graphics.DrawString("VLOC", model, ink, 169, 220);
            graphics.DrawString("GPS", model, ink, 847, 276);
        }

        private void DrawScreen(Graphics graphics)
        {
            Gns430LcdState state = new()
            {
                Snapshot = snapshot,
                Page = page,
                PageGroup = pageGroup,
                CursorActive = cursorActive,
                SelectedIndex = selectedIndex,
                DetailScrollLine = detailScrollLine,
                ResponseIndex = responseIndex,
                ZoomLevel = zoomLevel,
                LogonCode = logonCode,
                LogonCharacter = logonCharacter,
                TransientStatus = DateTime.UtcNow < transientStatusUntilUtc ? transientStatus : string.Empty,
                MenuItems = MenuItems(),
                Workflow = workflow,
                WorkflowCharacter = workflowCharacter,
                LoadSession = loadSession,
                OperationBusy = operationBusy,
                OperationStatus = operationStatus
            };
            using Bitmap lcd = Gns430LcdRenderer.Render(state);
            InterpolationMode previousInterpolation = graphics.InterpolationMode;
            PixelOffsetMode previousOffset = graphics.PixelOffsetMode;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(lcd, ScreenBounds);
            graphics.InterpolationMode = previousInterpolation;
            graphics.PixelOffsetMode = previousOffset;
        }

        private void DrawRadioStrip(Graphics graphics)
        {
            RectangleF strip = new(ScreenBounds.X, ScreenBounds.Y, MainScreenBounds.X - ScreenBounds.X, ScreenBounds.Height);
            using Brush blue = new SolidBrush(Color.FromArgb(0, 83, 166));
            using Brush black = new SolidBrush(Color.Black);
            using Brush white = new SolidBrush(ScreenWhite);
            using Brush cyan = new SolidBrush(ScreenCyan);
            using Brush green = new SolidBrush(ScreenGreen);
            using Brush amber = new SolidBrush(ScreenAmber);
            using Pen line = new(ScreenWhite, 1);
            using Font label = DisplayFont(6.5f, true);
            using Font value = DisplayFont(10.8f, true);
            using Font mode = DisplayFont(7.2f, true);

            graphics.FillRectangle(blue, strip);
            graphics.DrawString("DLK", label, cyan, strip.X + 5, strip.Y + 4);
            graphics.DrawString(Truncate(string.IsNullOrWhiteSpace(snapshot.Callsign) ? "--------" : snapshot.Callsign, 8), value, white, strip.X + 5, strip.Y + 18);
            graphics.DrawLine(line, strip.X + 3, strip.Y + 48, strip.Right - 3, strip.Y + 48);
            graphics.DrawString("ATC", label, cyan, strip.X + 5, strip.Y + 54);
            graphics.DrawString(Truncate(string.IsNullOrWhiteSpace(snapshot.CurrentAtcUnit) ? "----" : snapshot.CurrentAtcUnit, 8), value, green, strip.X + 5, strip.Y + 68);
            graphics.DrawLine(line, strip.X + 3, strip.Y + 100, strip.Right - 3, strip.Y + 100);
            graphics.DrawString("LOG", label, cyan, strip.X + 5, strip.Y + 106);
            graphics.DrawString(Truncate(string.IsNullOrWhiteSpace(snapshot.PendingLogon) ? "----" : snapshot.PendingLogon, 8), value, snapshot.Connected ? green : amber, strip.X + 5, strip.Y + 120);

            RectangleF modeBox = new(strip.X + 4, strip.Bottom - 71, strip.Width - 8, 27);
            graphics.FillRectangle(black, modeBox);
            graphics.DrawRectangle(line, modeBox.X, modeBox.Y, modeBox.Width, modeBox.Height);
            DrawCentered(graphics, snapshot.Connected ? "ENR" : "STBY", mode, snapshot.Connected ? green : amber, modeBox);

            RectangleF gpsBox = new(strip.X, strip.Bottom - 22, strip.Width, 22);
            graphics.FillRectangle(black, gpsBox);
            graphics.DrawString("GPS", mode, green, gpsBox.X + 6, gpsBox.Y + 4);
        }

        private void DrawStatusPage(Graphics graphics)
        {
            DrawPageHeader(graphics, "DATALINK STATUS", "");
            string connection = snapshot.Connected ? "CONNECTED" : "STANDBY";
            string callsign = string.IsNullOrWhiteSpace(snapshot.Callsign) ? "--------" : snapshot.Callsign;
            string station = string.IsNullOrWhiteSpace(snapshot.CurrentAtcUnit) ? "----" : snapshot.CurrentAtcUnit;

            using Brush black = new SolidBrush(Color.Black);
            using Brush green = new SolidBrush(ScreenGreen);
            using Brush magenta = new SolidBrush(ScreenMagenta);
            using Font route = DisplayFont(10.5f, true);
            RectangleF routeBox = new(MainScreenBounds.X + 6, MainScreenBounds.Y + 34, MainScreenBounds.Width - 12, 34);
            graphics.FillRectangle(black, routeBox);
            graphics.DrawString(Truncate(callsign, 8), route, green, routeBox.X + 8, routeBox.Y + 7);
            DrawCentered(graphics, "→", route, magenta, new RectangleF(routeBox.X + 156, routeBox.Y, 70, routeBox.Height));
            SizeF stationSize = graphics.MeasureString(Truncate(station, 8), route);
            graphics.DrawString(Truncate(station, 8), route, green, routeBox.Right - stationSize.Width - 8, routeBox.Y + 7);

            float cellWidth = (MainScreenBounds.Width - 18) / 2f;
            DrawDataCell(graphics, new RectangleF(MainScreenBounds.X + 6, MainScreenBounds.Y + 76, cellWidth, 61), "MESSAGES", snapshot.Messages.Count.ToString(), cursorActive && selectedIndex == 0, ScreenGreen);
            DrawDataCell(graphics, new RectangleF(MainScreenBounds.X + 12 + cellWidth, MainScreenBounds.Y + 76, cellWidth, 61), "CPDLC LOGON", station, cursorActive && selectedIndex == 1, ScreenGreen);
            DrawDataCell(graphics, new RectangleF(MainScreenBounds.X + 6, MainScreenBounds.Y + 143, cellWidth, 61), "VATSIM", connection, cursorActive && selectedIndex == 2, snapshot.Connected ? ScreenGreen : ScreenAmber);
            DrawDataCell(graphics, new RectangleF(MainScreenBounds.X + 12 + cellWidth, MainScreenBounds.Y + 143, cellWidth, 61), "ATC REQUEST", "SELECT", cursorActive && selectedIndex == 3, ScreenWhite);
            DrawFooter(graphics, cursorActive ? "CRSR" : "", "ENT SELECT");
        }

        private void DrawMessageListPage(Graphics graphics)
        {
            DrawPageHeader(graphics, "DATALINK MESSAGES", snapshot.Messages.Count.ToString());
            if (snapshot.Messages.Count == 0)
            {
                using Font emptyFont = DisplayFont(10.5f, true);
                using Brush emptyBrush = new SolidBrush(ScreenWhite);
                DrawCentered(graphics, "NO MESSAGES", emptyFont, emptyBrush, new RectangleF(MainScreenBounds.X, MainScreenBounds.Y + 100, MainScreenBounds.Width, 35));
                DrawFooter(graphics, "MSG", "CLR RETURN");
                return;
            }

            int first = Math.Max(0, selectedIndex - 5);
            int last = Math.Min(snapshot.Messages.Count, first + 7);
            for (int index = first; index < last; index++)
            {
                Gns430MessageSnapshot message = snapshot.Messages[index];
                int row = index - first;
                string direction = message.Outbound ? ">" : message.Unread ? "*" : "<";
                string title = direction + " " + Truncate(message.Type, 7) + " " + Truncate(message.Station, 8);
                string preview = CollapseWhitespace(message.Text);
                DrawMessageRow(graphics, row, title, preview, index == selectedIndex);
            }

            DrawScrollBar(graphics, first, snapshot.Messages.Count, 7);
            DrawFooter(graphics, cursorActive ? "CRSR" : "MSG", $"{selectedIndex + 1}/{snapshot.Messages.Count}  ENT OPEN");
        }

        private void DrawMessageDetailPage(Graphics graphics)
        {
            Gns430MessageSnapshot message = SelectedMessage();
            if (message == null)
            {
                SetPage(Gns430Page.Messages, true);
                return;
            }

            DrawPageHeader(graphics, message.Outbound ? "UPLINK SENT" : "UPLINK RECEIVED", Truncate(message.Station, 8));
            List<string> lines = WrapText(CollapseWhitespace(message.Text), zoomLevel == 2 ? 30 : zoomLevel == 0 ? 45 : 38);
            int visibleLines = message.Responses.Count > 0 ? 7 : 10;
            detailScrollLine = Math.Clamp(detailScrollLine, 0, Math.Max(0, lines.Count - visibleLines));
            using Font text = DisplayFont(zoomLevel == 2 ? 9.5f : zoomLevel == 0 ? 7f : 8.1f, false);
            using Brush white = new SolidBrush(ScreenWhite);
            using Brush cyan = new SolidBrush(ScreenCyan);
            using Font typeFont = DisplayFont(7f, true);
            graphics.DrawString(Truncate(message.Type, 24), typeFont, cyan, MainScreenBounds.X + 8, MainScreenBounds.Y + 36);
            float y = MainScreenBounds.Y + 55;
            for (int i = detailScrollLine; i < Math.Min(lines.Count, detailScrollLine + visibleLines); i++)
            {
                graphics.DrawString(lines[i], text, white, MainScreenBounds.X + 8, y);
                y += zoomLevel == 2 ? 22 : zoomLevel == 0 ? 15 : 18;
            }

            if (message.Responses.Count > 0)
            {
                string response = message.Responses[Math.Clamp(responseIndex, 0, message.Responses.Count - 1)];
                DrawValueBox(graphics, new RectangleF(MainScreenBounds.X + 92, ScreenBounds.Bottom - 54, MainScreenBounds.Width - 184, 27), response, cursorActive);
            }
            DrawFooter(graphics, "MSG", message.Responses.Count > 0 ? "SMALL RESPONSE  ENT SEND" : "ENT READ  CLR LIST");
        }

        private void DrawLogonPage(Graphics graphics)
        {
            DrawPageHeader(graphics, "CPDLC DIRECT LOGON", "");
            using Font label = DisplayFont(7.5f, true);
            using Brush white = new SolidBrush(ScreenWhite);
            using Brush cyan = new SolidBrush(ScreenCyan);
            graphics.DrawString("LOGON FACILITY", label, cyan, MainScreenBounds.X + 34, MainScreenBounds.Y + 58);

            float startX = MainScreenBounds.X + 123;
            for (int i = 0; i < 4; i++)
            {
                bool selected = cursorActive && i == logonCharacter;
                DrawValueBox(graphics, new RectangleF(startX + (i * 39), MainScreenBounds.Y + 81, 34, 35), logonCode[i].ToString(), selected);
            }

            string current = string.IsNullOrWhiteSpace(snapshot.CurrentAtcUnit) ? "NONE" : snapshot.CurrentAtcUnit;
            string pending = string.IsNullOrWhiteSpace(snapshot.PendingLogon) ? "NONE" : snapshot.PendingLogon;
            DrawDataCell(graphics, new RectangleF(MainScreenBounds.X + 36, MainScreenBounds.Y + 138, MainScreenBounds.Width - 72, 42), "CURRENT FACILITY", current, false, ScreenGreen);
            DrawDataCell(graphics, new RectangleF(MainScreenBounds.X + 36, MainScreenBounds.Y + 185, MainScreenBounds.Width - 72, 42), "PENDING LOGON", pending, false, ScreenAmber);
            DrawFooter(graphics, cursorActive ? "CRSR" : "", "SMALL CHAR  LARGE POS  ENT");
        }

        private void DrawMenuPage(Graphics graphics)
        {
            List<string> items = MenuItems();
            RectangleF menu = new(MainScreenBounds.X + 24, MainScreenBounds.Y + 18, MainScreenBounds.Width - 48, MainScreenBounds.Height - 48);
            using Brush black = new SolidBrush(Color.Black);
            using Pen border = new(ScreenWhite, 1);
            using Font heading = DisplayFont(7.2f, true);
            using Brush green = new SolidBrush(ScreenGreen);
            graphics.FillRectangle(black, menu);
            graphics.DrawRectangle(border, menu.X, menu.Y, menu.Width, menu.Height);
            graphics.DrawString("PAGE MENU", heading, green, menu.X + 8, menu.Y + 6);
            int first = Math.Max(0, selectedIndex - 5);
            for (int index = first; index < Math.Min(items.Count, first + 7); index++)
            {
                int row = index - first;
                DrawMenuRow(graphics, menu, row, items[index], index == selectedIndex);
            }
            DrawScrollBar(graphics, first, items.Count, 7);
            DrawFooter(graphics, "MENU", "LARGE SELECT  ENT");
        }

        private void DrawHelpPage(Graphics graphics)
        {
            DrawPageHeader(graphics, "INPUT / CONTROL SETUP", "");
            string[] lines =
            {
                "PANEL KEYBOARD",
                "LEFT/RIGHT   LARGE RIGHT KNOB",
                "DOWN/UP      SMALL RIGHT KNOB",
                "SPACE        PUSH CRSR",
                "ENTER / ESC  ENT / CLR",
                "",
                "MOBIFLIGHT PROJECT INCLUDED",
                "PRIVATE EASYCPDLC L-VARS ONLY",
                "",
                "NO GARMIN OR AIRCRAFT GPS EVENTS"
            };
            using Font font = DisplayFont(6.9f, false);
            using Brush white = new SolidBrush(ScreenWhite);
            using Brush cyan = new SolidBrush(ScreenCyan);
            float y = MainScreenBounds.Y + 39;
            int lineNumber = 0;
            foreach (string line in lines.Skip(detailScrollLine).Take(12))
            {
                graphics.DrawString(line, font, lineNumber == 0 || line.StartsWith("MOBIFLIGHT") ? cyan : white, MainScreenBounds.X + 9, y);
                y += 16;
                lineNumber++;
            }
            DrawFooter(graphics, cursorActive ? "CRSR" : "", "PUSH CRSR THEN LARGE SCROLL");
        }

        private void DrawPageHeader(Graphics graphics, string title, string pageNumber)
        {
            using Brush black = new SolidBrush(Color.Black);
            using Brush green = new SolidBrush(ScreenGreen);
            using Brush white = new SolidBrush(ScreenWhite);
            using Font font = DisplayFont(7.5f, true);
            RectangleF header = new(MainScreenBounds.X + 5, MainScreenBounds.Y + 4, MainScreenBounds.Width - 10, 25);
            graphics.FillRectangle(black, header);
            graphics.DrawString(title, font, green, header.X + 7, header.Y + 5);
            SizeF size = graphics.MeasureString(pageNumber, font);
            graphics.DrawString(pageNumber, font, white, header.Right - size.Width - 7, header.Y + 5);
        }

        private void DrawDataCell(Graphics graphics, RectangleF bounds, string label, string value, bool selected, Color valueColor)
        {
            using Brush fill = new SolidBrush(selected ? ScreenWhite : Color.Black);
            using Pen border = new(ScreenWhite, 1);
            using Brush labelBrush = new SolidBrush(selected ? Color.Black : ScreenCyan);
            using Brush valueBrush = new SolidBrush(selected ? Color.Black : valueColor);
            using Font labelFont = DisplayFont(6.2f, true);
            using Font valueFont = DisplayFont(8.5f, true);
            graphics.FillRectangle(fill, bounds);
            graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            graphics.DrawString(label, labelFont, labelBrush, bounds.X + 5, bounds.Y + 3);
            DrawCentered(graphics, value, valueFont, valueBrush, new RectangleF(bounds.X + 4, bounds.Y + 18, bounds.Width - 8, bounds.Height - 21));
        }

        private void DrawMessageRow(Graphics graphics, int row, string title, string preview, bool selected)
        {
            float y = MainScreenBounds.Y + 35 + (row * 28);
            RectangleF bounds = new(MainScreenBounds.X + 6, y, MainScreenBounds.Width - 14, 26);
            using Brush fill = new SolidBrush(selected && cursorActive ? ScreenWhite : Color.Black);
            using Pen border = new(ScreenWhite, 0.7f);
            using Font titleFont = DisplayFont(6.5f, true);
            using Font previewFont = DisplayFont(6.2f, false);
            using Brush titleBrush = new SolidBrush(selected && cursorActive ? Color.Black : ScreenGreen);
            using Brush previewBrush = new SolidBrush(selected && cursorActive ? Color.Black : ScreenWhite);
            graphics.FillRectangle(fill, bounds);
            graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            graphics.DrawString(title, titleFont, titleBrush, bounds.X + 4, bounds.Y + 2);
            graphics.DrawString(Truncate(preview, 31), previewFont, previewBrush, bounds.X + 145, bounds.Y + 3);
        }

        private void DrawMenuRow(Graphics graphics, RectangleF menuBounds, int row, string text, bool selected)
        {
            float y = menuBounds.Y + 26 + (row * 27);
            RectangleF bounds = new(menuBounds.X + 5, y, menuBounds.Width - 10, 25);
            using Brush fill = new SolidBrush(selected ? ScreenWhite : Color.Black);
            using Brush brush = new SolidBrush(selected ? Color.Black : ScreenWhite);
            using Font font = DisplayFont(6.8f, true);
            graphics.FillRectangle(fill, bounds);
            graphics.DrawString(text, font, brush, bounds.X + 6, bounds.Y + 5);
        }

        private void DrawValueBox(Graphics graphics, RectangleF bounds, string value, bool selected)
        {
            using Brush fill = new SolidBrush(selected ? ScreenWhite : Color.Black);
            using Pen border = new(ScreenWhite, 1f);
            using Brush text = new SolidBrush(selected ? Color.Black : ScreenGreen);
            using Font font = DisplayFont(bounds.Height > 31 ? 13 : 7.8f, true);
            graphics.FillRectangle(fill, bounds);
            graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            DrawCentered(graphics, value, font, text, bounds);
        }

        private static void DrawScrollBar(Graphics graphics, int firstItem, int totalItems, int visibleItems)
        {
            if (totalItems <= visibleItems || totalItems <= 0)
            {
                return;
            }

            RectangleF track = new(MainScreenBounds.Right - 7, MainScreenBounds.Y + 35, 4, MainScreenBounds.Height - 62);
            float thumbHeight = Math.Max(13, track.Height * visibleItems / totalItems);
            float travel = Math.Max(0, track.Height - thumbHeight);
            float maximumFirst = Math.Max(1, totalItems - visibleItems);
            float thumbY = track.Y + (travel * Math.Min(firstItem, (int)maximumFirst) / maximumFirst);
            using Brush trackBrush = new SolidBrush(Color.Black);
            using Brush thumbBrush = new SolidBrush(ScreenWhite);
            graphics.FillRectangle(trackBrush, track);
            graphics.FillRectangle(thumbBrush, new RectangleF(track.X - 1, thumbY, track.Width + 2, thumbHeight));
        }

        private void DrawFooter(Graphics graphics, string group, string hint)
        {
            using Brush black = new SolidBrush(Color.Black);
            using Brush green = new SolidBrush(ScreenGreen);
            using Brush white = new SolidBrush(ScreenWhite);
            using Brush amber = new SolidBrush(ScreenAmber);
            using Font font = DisplayFont(6.1f, true);
            RectangleF footer = new(MainScreenBounds.X, ScreenBounds.Bottom - 22, MainScreenBounds.Width, 22);
            graphics.FillRectangle(black, footer);
            string annunciator = snapshot.Messages.Any(message => message.Unread) ? "MSG" : group;
            graphics.DrawString(annunciator, font, annunciator == "MSG" ? amber : green, footer.X + 5, footer.Y + 4);
            graphics.DrawString(hint, font, white, footer.X + 62, footer.Y + 4);
            DrawPageIndicator(graphics, footer);
        }

        private void DrawPageIndicator(Graphics graphics, RectangleF footer)
        {
            Gns430PageGroup visibleGroup = page == Gns430Page.Menu ? groupBeforeOverlay : pageGroup;
            Gns430Page[] pages = PagesForGroup(visibleGroup);
            int current = Math.Max(0, Array.IndexOf(pages, page));
            string label = visibleGroup.ToString().ToUpperInvariant();
            using Font font = DisplayFont(6.2f, true);
            using Brush white = new SolidBrush(ScreenWhite);
            SizeF labelSize = graphics.MeasureString(label, font);
            float squaresWidth = (pages.Length * 8) + 3;
            float startX = footer.Right - labelSize.Width - squaresWidth - 8;
            for (int i = 0; i < pages.Length; i++)
            {
                RectangleF square = new(startX + (i * 8), footer.Y + 7, 6, 7);
                using Brush fill = new SolidBrush(i == current ? ScreenWhite : ScreenBlue);
                using Pen outline = new(ScreenWhite, 1);
                graphics.FillRectangle(fill, square);
                graphics.DrawRectangle(outline, square.X, square.Y, square.Width, square.Height);
            }
            graphics.DrawString(label, font, white, footer.Right - labelSize.Width - 4, footer.Y + 4);
        }

        private void DrawPanelButtons(Graphics graphics)
        {
            foreach (PanelButton button in panelButtons)
            {
                bool range = button.Label == "▽" || button.Label == "△";
                using GraphicsPath shape = RoundedRectangle(button.Bounds, Math.Min(8, button.Bounds.Height / 2));
                using LinearGradientBrush face = new(button.Bounds, Color.FromArgb(235, 235, 222), Color.FromArgb(133, 136, 130), LinearGradientMode.Vertical);
                using Pen edge = new(Color.FromArgb(15, 18, 17), 1.3f);
                using Font font = new(FontFamily.GenericSansSerif, range ? 9f : 7.1f, FontStyle.Bold, GraphicsUnit.Point);
                using Brush text = new SolidBrush(Color.FromArgb(16, 18, 16));
                graphics.FillPath(face, shape);
                graphics.DrawPath(edge, shape);
                DrawCentered(graphics, button.Label, font, text, button.Bounds);
            }

            using Font tiny = new(FontFamily.GenericSansSerif, 6.2f, FontStyle.Bold, GraphicsUnit.Point);
            using Brush ink = new SolidBrush(Color.FromArgb(24, 26, 24));
            graphics.DrawString("RNG", tiny, ink, 852, 91);
            graphics.DrawString("DEFAULT", tiny, ink, 764, 174);
            graphics.DrawString("NAV", tiny, ink, 770, 183);
        }

        private void DrawKnob(Graphics graphics, PointF center, string top, string bottom)
        {
            RectangleF outer = new(center.X - 49, center.Y - 49, 98, 98);
            RectangleF middle = new(center.X - 36, center.Y - 36, 72, 72);
            RectangleF inner = new(center.X - 22, center.Y - 22, 44, 44);
            using LinearGradientBrush outerFill = new(outer, Color.FromArgb(214, 215, 205), Color.FromArgb(78, 80, 76), LinearGradientMode.ForwardDiagonal);
            using LinearGradientBrush middleFill = new(middle, Color.FromArgb(182, 184, 176), Color.FromArgb(55, 58, 55), LinearGradientMode.BackwardDiagonal);
            using Brush innerFill = new SolidBrush(Color.FromArgb(207, 209, 198));
            using Pen dark = new(Color.FromArgb(14, 15, 14), 2);
            graphics.FillEllipse(outerFill, outer);
            graphics.DrawEllipse(dark, outer);
            graphics.FillEllipse(middleFill, middle);
            graphics.DrawEllipse(dark, middle);
            graphics.FillEllipse(innerFill, inner);
            graphics.DrawEllipse(dark, inner);

            for (int i = 0; i < 16; i++)
            {
                double angle = i * Math.PI * 2 / 16;
                PointF start = new(center.X + (float)Math.Cos(angle) * 40, center.Y + (float)Math.Sin(angle) * 40);
                PointF end = new(center.X + (float)Math.Cos(angle) * 47, center.Y + (float)Math.Sin(angle) * 47);
                graphics.DrawLine(dark, start, end);
            }

            using Font font = new(FontFamily.GenericSansSerif, 5.5f, FontStyle.Bold, GraphicsUnit.Point);
            using Brush text = new SolidBrush(Color.Black);
            DrawCentered(graphics, top, font, text, new RectangleF(center.X - 22, center.Y - 12, 44, 11));
            DrawCentered(graphics, bottom, font, text, new RectangleF(center.X - 22, center.Y + 1, 44, 11));
        }

        private void DrawSmallVolumeKnob(Graphics graphics, PointF center, string symbol, string label)
        {
            RectangleF body = new(center.X - 18, center.Y - 18, 36, 36);
            using LinearGradientBrush fill = new(body, Color.FromArgb(224, 224, 212), Color.FromArgb(86, 88, 84), LinearGradientMode.ForwardDiagonal);
            using Pen edge = new(Color.FromArgb(25, 27, 25), 1.4f);
            using Brush ink = new SolidBrush(Color.FromArgb(20, 22, 20));
            using Font symbolFont = new(FontFamily.GenericSansSerif, 7f, FontStyle.Bold, GraphicsUnit.Point);
            using Font labelFont = new(FontFamily.GenericSansSerif, 5.5f, FontStyle.Bold, GraphicsUnit.Point);
            graphics.FillEllipse(fill, body);
            graphics.DrawEllipse(edge, body);
            DrawCentered(graphics, symbol, symbolFont, ink, body);
            DrawCentered(graphics, label, labelFont, ink, new RectangleF(center.X - 30, center.Y + 20, 60, 13));
        }

        private static void DrawFastener(Graphics graphics, float x, float y)
        {
            RectangleF screw = new(x - 4, y - 4, 8, 8);
            using Brush fill = new SolidBrush(Color.FromArgb(93, 95, 91));
            using Pen edge = new(Color.FromArgb(40, 42, 40), 1);
            graphics.FillEllipse(fill, screw);
            graphics.DrawEllipse(edge, screw);
            graphics.DrawLine(edge, x - 2.5f, y, x + 2.5f, y);
        }

        private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            float diameter = radius * 2;
            GraphicsPath path = new();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Font DisplayFont(float baseSize, bool bold)
        {
            float adjustment = zoomLevel switch { 0 => -0.3f, 2 => 0.5f, _ => 0f };
            return new Font(displayFontFamily, Math.Max(6, baseSize + adjustment), bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
        }

        private static void DrawCentered(Graphics graphics, string text, Font font, Brush brush, RectangleF bounds)
        {
            using StringFormat format = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            graphics.DrawString(text ?? string.Empty, font, brush, bounds, format);
        }

        private void PanelMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            PointF logical = ToLogicalPoint(e.Location);

            foreach (PanelButton button in panelButtons)
            {
                if (button.Bounds.Contains(logical))
                {
                    pressedPanelButton = button;
                    SetArtworkFeedback(
                        button.ArtworkControl,
                        button.Command == Gns430Command.RangeOut ? "decrease-pressed"
                            : button.Command == Gns430Command.RangeIn ? "increase-pressed"
                            : "pressed",
                        held: true);
                    Capture = true;
                    Invalidate();
                    return;
                }
            }

            if (KnobPushCommandAt(logical, new PointF(878, 326)) == Gns430Command.CursorPush)
            {
                pressedCursor = true;
                SetArtworkFeedback("right_encoder", "pushed", held: true);
                Capture = true;
                Invalidate();
                return;
            }

            if (ScreenBounds.Contains(logical))
            {
                pressedScreen = true;
                Capture = true;
            }
        }

        private void PanelMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            PointF logical = ToLogicalPoint(e.Location);
            PanelButton releasedButton = pressedPanelButton;
            bool releasedCursor = pressedCursor;
            bool releasedScreen = pressedScreen;
            ReleasePointerPress();

            if (releasedButton != null && releasedButton.Bounds.Contains(logical))
            {
                ExecuteCommand(releasedButton.Command);
                ClearArtworkFeedback();
            }
            else if (releasedCursor && KnobPushCommandAt(logical, new PointF(878, 326)) == Gns430Command.CursorPush)
            {
                ExecuteCommand(Gns430Command.CursorPush);
                ClearArtworkFeedback();
            }
            else if (releasedScreen && ScreenBounds.Contains(logical))
            {
                HandleScreenClick(logical);
            }

            Invalidate();
        }

        private void HandleCompanionCommand(Gns430Command command)
        {
            if (command >= Gns430Command.DcduLeftLsk1)
            {
                if (preferences.DcduCompanionMode)
                {
                    backend.HandleDcduCompanionCommand(command);
                }

                return;
            }

            if (!preferences.DcduCompanionMode)
            {
                ExecuteCommand(command);
            }
        }

        internal bool DcduCompanionMode => preferences.DcduCompanionMode;

        internal bool SetDcduCompanionMode(bool enabled, out string error)
        {
            error = string.Empty;
            preferences.DcduCompanionMode = enabled;
            if (enabled)
            {
                preferences.CompanionModuleEnabled = true;
            }

            if (enabled && !companionInput.Enabled)
            {
                if (!companionInput.TryEnable(Handle, WmAppSimConnect, out error))
                {
                    preferences.Save(Bounds);
                    return true;
                }
            }

            preferences.Save(Bounds);
            Invalidate();
            return true;
        }

        private void PanelMouseWheel(object sender, MouseEventArgs e)
        {
            Gns430Command command = KnobWheelCommandAt(ToLogicalPoint(e.Location), new PointF(878, 326), e.Delta);
            if (command == Gns430Command.None)
            {
                return;
            }

            int detents = Math.Max(1, Math.Abs(e.Delta) / Math.Max(1, SystemInformation.MouseWheelScrollDelta));
            for (int index = 0; index < detents; index++)
            {
                ExecuteCommand(command);
            }
        }

        private void PanelMouseCaptureChanged(object sender, EventArgs e)
        {
            if (!Capture && (pressedPanelButton != null || pressedCursor || pressedScreen))
            {
                ReleasePointerPress();
                Invalidate();
            }
        }

        private void ReleasePointerPress()
        {
            pressedPanelButton = null;
            pressedCursor = false;
            pressedScreen = false;
            ClearArtworkFeedback();
            if (Capture)
            {
                Capture = false;
            }
        }

        private PointF ToLogicalPoint(Point point)
        {
            return new PointF(
                point.X * LogicalWidth / (float)Math.Max(1, ClientSize.Width),
                point.Y * LogicalHeight / (float)Math.Max(1, ClientSize.Height));
        }

        internal static Gns430Command KnobPushCommandAt(PointF point, PointF center)
        {
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
            return distance < 27 ? Gns430Command.CursorPush : Gns430Command.None;
        }

        internal static Gns430Command KnobWheelCommandAt(PointF point, PointF center, int delta)
        {
            if (delta == 0)
            {
                return Gns430Command.None;
            }

            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
            if (distance > 78)
            {
                return Gns430Command.None;
            }

            if (distance < 49)
            {
                return delta < 0 ? Gns430Command.SmallRightDecrease : Gns430Command.SmallRightIncrease;
            }

            return delta < 0 ? Gns430Command.LargeRightDecrease : Gns430Command.LargeRightIncrease;
        }

        private void HandleScreenClick(PointF point)
        {
            float screenX = (point.X - ScreenBounds.X) * Gns430LcdRenderer.Width / ScreenBounds.Width;
            float screenY = (point.Y - ScreenBounds.Y) * Gns430LcdRenderer.Height / ScreenBounds.Height;
            if (page == Gns430Page.Status && screenY >= 10 && screenY < 100 && screenX >= 59)
            {
                selectedIndex = screenY < 25 ? 0
                    : screenY < 52 ? 2
                    : screenY < 78 ? 1
                    : 3;
                cursorActive = true;
                ActivateSelection();
            }
            else if (page == Gns430Page.Messages && screenY >= 13 && screenY < 111 && screenX >= 61)
            {
                int first = Math.Max(0, selectedIndex - 5);
                selectedIndex = Math.Clamp(first + (int)((screenY - 13) / 14), 0, Math.Max(0, snapshot.Messages.Count - 1));
                cursorActive = true;
                ActivateSelection();
            }
            else if (page == Gns430Page.Menu && screenY >= 40 && screenY < 103 && screenX >= 72 && screenX <= 223)
            {
                int first = Math.Max(0, selectedIndex - 5);
                selectedIndex = Math.Clamp(first + (int)((screenY - 40) / 9), 0, MenuItems().Count - 1);
                ActivateSelection();
            }
            else if ((page == Gns430Page.AtcMenu || page == Gns430Page.AocMenu) && screenY >= 12 && screenY < 112 && screenX >= 59)
            {
                int count = page == Gns430Page.AtcMenu ? AtcMenuItems.Length : AocMenuItems.Length;
                int first = Math.Max(0, selectedIndex - 5);
                selectedIndex = Math.Clamp(first + (int)((screenY - 12) / 14), 0, count - 1);
                cursorActive = true;
                ActivateSelection();
            }
            else if ((page == Gns430Page.AtcRequest || page == Gns430Page.AocRequest) && workflow != null && screenY >= 10 && screenY < 113 && screenX >= 59)
            {
                int first = Math.Max(0, selectedIndex - 2);
                selectedIndex = Math.Clamp(first + (int)((screenY - 11) / 22), 0, workflow.Fields.Count - 1);
                workflowCharacter = 0;
                cursorActive = true;
            }
            else if (page == Gns430Page.LoadControl && loadSession != null && screenY >= 10 && screenY < 105 && screenX >= 59)
            {
                int first = Math.Max(0, selectedIndex - 4);
                selectedIndex = Math.Clamp(first + (int)((screenY - 12) / 15), 0, loadSession.FieldCount - 1);
                cursorActive = true;
            }
        }

        private List<PanelButton> CreatePanelButtons()
        {
            return new List<PanelButton>
            {
                new() { Bounds = new RectangleF(153, 51, 54, 70), Label = "COM", ArtworkControl = "com_flip", Command = Gns430Command.Cdi },
                new() { Bounds = new RectangleF(153, 132, 54, 72), Label = "VLOC", ArtworkControl = "vloc_flip", Command = Gns430Command.Message },
                new() { Bounds = new RectangleF(779, 151, 76, 55), Label = "CLR", ArtworkControl = "clear", Command = Gns430Command.Clear },
                new() { Bounds = new RectangleF(779, 94, 76, 53), Label = "D->", ArtworkControl = "direct_to", Command = Gns430Command.DirectTo },
                new() { Bounds = new RectangleF(781, 40, 75, 54), Label = "RNG-", ArtworkControl = "range", Command = Gns430Command.RangeOut },
                new() { Bounds = new RectangleF(856, 40, 76, 54), Label = "RNG+", ArtworkControl = "range", Command = Gns430Command.RangeIn },
                new() { Bounds = new RectangleF(856, 94, 76, 53), Label = "MENU", ArtworkControl = "menu", Command = Gns430Command.Menu },
                new() { Bounds = new RectangleF(856, 151, 76, 55), Label = "ENT", ArtworkControl = "enter", Command = Gns430Command.Enter },
                new() { Bounds = new RectangleF(243, 343, 82, 50), Label = "CDI", ArtworkControl = "cdi", Command = Gns430Command.Cdi },
                new() { Bounds = new RectangleF(350, 343, 83, 50), Label = "OBS", ArtworkControl = "obs", Command = Gns430Command.Obs },
                new() { Bounds = new RectangleF(457, 343, 84, 50), Label = "MSG", ArtworkControl = "msg", Command = Gns430Command.Message },
                new() { Bounds = new RectangleF(567, 343, 84, 50), Label = "FPL", ArtworkControl = "fpl", Command = Gns430Command.FlightPlan },
                new() { Bounds = new RectangleF(674, 343, 88, 50), Label = "PROC", ArtworkControl = "proc", Command = Gns430Command.Procedure }
            };
        }

        private void PanelFormClosing(object sender, FormClosingEventArgs e)
        {
            preferences.Save(Bounds);
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        internal static bool ShouldPreserveMessageSelection(Gns430Page targetPage)
        {
            return targetPage == Gns430Page.Messages || targetPage == Gns430Page.MessageDetail;
        }

        internal static int Wrap(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int result = value % count;
            return result < 0 ? result + count : result;
        }

        internal static string CollapseWhitespace(string value)
        {
            return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
        }

        internal static List<string> WrapText(string value, int maxCharacters)
        {
            List<string> lines = new();
            string remaining = CollapseWhitespace(value);
            int width = Math.Max(8, maxCharacters);

            while (remaining.Length > width)
            {
                int split = remaining.LastIndexOf(' ', width);
                if (split < 1)
                {
                    split = width;
                }

                lines.Add(remaining.Substring(0, split).Trim());
                remaining = remaining.Substring(split).TrimStart();
            }

            if (remaining.Length > 0 || lines.Count == 0)
            {
                lines.Add(remaining);
            }
            return lines;
        }

        private static string Truncate(string value, int maxLength)
        {
            string clean = value ?? string.Empty;
            return clean.Length <= maxLength ? clean : clean.Substring(0, Math.Max(1, maxLength - 1)) + "…";
        }
    }

    internal static class Gns430FontLoader
    {
        private static readonly PrivateFontCollection Fonts = LoadFonts();

        internal static FontFamily Family => Fonts.Families.FirstOrDefault()
            ?? FontFamily.GenericMonospace;

        private static PrivateFontCollection LoadFonts()
        {
            PrivateFontCollection collection = new();
            AddFont(collection, Properties.Resources.B612Mono_Regular);
            AddFont(collection, Properties.Resources.B612Mono_Bold);
            return collection;
        }

        private static void AddFont(PrivateFontCollection collection, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            IntPtr memory = Marshal.AllocCoTaskMem(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, memory, bytes.Length);
                collection.AddMemoryFont(memory, bytes.Length);
            }
            finally
            {
                Marshal.FreeCoTaskMem(memory);
            }
        }
    }
}
