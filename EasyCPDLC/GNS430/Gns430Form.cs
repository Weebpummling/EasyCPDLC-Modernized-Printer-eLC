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
            KeyPreview = true;
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

            companionInput.CommandReceived += command => BeginInvoke(new Action(() => ExecuteCommand(command)));
            refreshTimer.Tick += RefreshTimerTick;
            refreshTimer.Start();

            Paint += PaintPanel;
            MouseDown += PanelMouseDown;
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys modifiers = keyData & Keys.Modifiers;
            Keys key = keyData & Keys.KeyCode;
            if (modifiers != Keys.None)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            Gns430Command command = key switch
            {
                Keys.Left => Gns430Command.LargeRightDecrease,
                Keys.Right => Gns430Command.LargeRightIncrease,
                Keys.Down => Gns430Command.SmallRightDecrease,
                Keys.Up => Gns430Command.SmallRightIncrease,
                Keys.Space => Gns430Command.CursorPush,
                Keys.Enter => Gns430Command.Enter,
                Keys.Escape => Gns430Command.Clear,
                Keys.Back => Gns430Command.Clear,
                Keys.M => Gns430Command.Menu,
                Keys.G => Gns430Command.Message,
                Keys.F => Gns430Command.FlightPlan,
                Keys.P => Gns430Command.Procedure,
                Keys.D => Gns430Command.DirectTo,
                Keys.O => Gns430Command.Obs,
                Keys.Add => Gns430Command.RangeIn,
                Keys.Oemplus => Gns430Command.RangeIn,
                Keys.Subtract => Gns430Command.RangeOut,
                Keys.OemMinus => Gns430Command.RangeOut,
                _ => Gns430Command.None
            };

            if (command == Gns430Command.None)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            ExecuteCommand(command);
            return true;
        }

        private void RefreshTimerTick(object sender, EventArgs e)
        {
            companionInput.UpdateStatus(snapshot, page, cursorActive);
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
                case Gns430Command.Nearest:
                    SetPage(
                        page == Gns430Page.Messages ? Gns430Page.Status : Gns430Page.Messages,
                        page != Gns430Page.Messages,
                        page == Gns430Page.Messages ? Gns430PageGroup.Nav : Gns430PageGroup.Nrst);
                    break;
                case Gns430Command.FlightPlan:
                    backend.Gns430OpenAtcRequests();
                    SetTransient("ATC MENU OPENED");
                    break;
                case Gns430Command.Procedure:
                    backend.Gns430OpenAocTelex();
                    SetTransient("AOC MENU OPENED");
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
                            backend.Gns430OpenAtcRequests();
                            SetTransient("ATC MENU OPENED");
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
                    backend.Gns430OpenAtcRequests();
                    SetTransient("ATC MENU OPENED");
                    break;
                case 2:
                    backend.Gns430OpenAocTelex();
                    SetTransient("AOC MENU OPENED");
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
                SetTransient("MSFS MODULE OFF");
                return;
            }

            if (companionInput.TryEnable(Handle, WmAppSimConnect, out string error))
            {
                preferences.CompanionModuleEnabled = true;
                SetTransient("WAITING FOR MODULE");
            }
            else
            {
                preferences.CompanionModuleEnabled = false;
                MessageBox.Show(this, error, "Companion module unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetTransient("MSFS MODULE OFFLINE");
            }
        }

        private void ClearOrBack()
        {
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
                Gns430PageGroup.Wpt => new[] { Gns430Page.Logon },
                Gns430PageGroup.Aux => new[] { Gns430Page.Help },
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

        private void SetArtworkFeedback(string control, string state)
        {
            activeArtworkControl = control ?? string.Empty;
            activeArtworkState = state ?? string.Empty;
            activeArtworkUntilUtc = string.IsNullOrWhiteSpace(activeArtworkControl)
                ? DateTime.MinValue
                : DateTime.UtcNow.AddMilliseconds(220);
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
                MenuItems = MenuItems()
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
            PointF logical = new(
                e.X * LogicalWidth / (float)Math.Max(1, ClientSize.Width),
                e.Y * LogicalHeight / (float)Math.Max(1, ClientSize.Height));

            foreach (PanelButton button in panelButtons)
            {
                if (button.Bounds.Contains(logical))
                {
                    ExecuteCommand(button.Command);
                    SetArtworkFeedback(
                        button.ArtworkControl,
                        button.Command == Gns430Command.RangeOut ? "decrease-pressed"
                            : button.Command == Gns430Command.RangeIn ? "increase-pressed"
                            : "pressed");
                    return;
                }
            }

            if (TryHandleKnob(logical, new PointF(878, 326)))
            {
                return;
            }

            if (ScreenBounds.Contains(logical))
            {
                HandleScreenClick(logical);
            }
        }

        private bool TryHandleKnob(PointF point, PointF center)
        {
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
            if (distance > 78)
            {
                return false;
            }

            if (distance < 27)
            {
                ExecuteCommand(Gns430Command.CursorPush);
            }
            else if (distance < 49)
            {
                ExecuteCommand(dx < 0 ? Gns430Command.SmallRightDecrease : Gns430Command.SmallRightIncrease);
            }
            else
            {
                ExecuteCommand(dx < 0 ? Gns430Command.LargeRightDecrease : Gns430Command.LargeRightIncrease);
            }
            return true;
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
