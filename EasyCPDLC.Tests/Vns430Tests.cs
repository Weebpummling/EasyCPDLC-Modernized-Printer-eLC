using EasyCPDLC.VNS430;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace EasyCPDLC.Tests
{
    public sealed class Vns430Tests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(18)]
        [InlineData(19)]
        [InlineData(38)]
        public void CompanionPackets_ValidateEveryDefinedCommand(byte value)
        {
            Vns430CompanionCommandPacket packet = new()
            {
                Magic = Vns430CompanionProtocol.Magic,
                Version = Vns430CompanionProtocol.Version,
                Sequence = 27,
                Command = value,
                Checksum = Vns430CompanionProtocol.CalculateCommandChecksum(27, value)
            };

            Assert.True(Vns430CompanionProtocol.TryReadCommand(packet, out Vns430Command command));
            Assert.Equal((Vns430Command)value, command);
        }

        [Fact]
        public void CompanionPackets_RejectDamageAndUnknownCommands()
        {
            Vns430CompanionCommandPacket packet = new()
            {
                Magic = Vns430CompanionProtocol.Magic,
                Version = Vns430CompanionProtocol.Version,
                Sequence = 5,
                Command = 99,
                Checksum = Vns430CompanionProtocol.CalculateCommandChecksum(5, 99)
            };

            Assert.False(Vns430CompanionProtocol.TryReadCommand(packet, out _));

            packet.Command = (uint)Vns430Command.Menu;
            packet.Checksum = 0;
            Assert.False(Vns430CompanionProtocol.TryReadCommand(packet, out _));
        }

        [Fact]
        public void CompanionProtocol_HasStableCrossLanguageLayoutAndPrivateNames()
        {
            Assert.Equal(20, Marshal.SizeOf<Vns430CompanionCommandPacket>());
            Assert.Equal(32, Marshal.SizeOf<Vns430CompanionStatusPacket>());
            Assert.StartsWith("EasyCPDLC.", Vns430CompanionProtocol.CommandClientDataName);
            Assert.Equal("EASYCPDLC_VNS_COMMAND", Vns430CompanionProtocol.CommandLVar);
            Assert.DoesNotContain("GPS_", Vns430CompanionProtocol.CommandClientDataName);
            Assert.DoesNotContain("AS430", Vns430CompanionProtocol.CommandClientDataName);
            Assert.Equal(1u << 3, Vns430CompanionProtocol.StatusDcduMode);
            Assert.Equal("EASYCPDLC_DCDU_MODE", Vns430CompanionProtocol.DcduModeLVar);
        }

        [Fact]
        public void CompanionModule_DcduInputsArePrivateAndModeGated()
        {
            string source = File.ReadAllText(Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "EasyCPDLC", "VNS430", "MSFS2024Module", "Bridge", "Sources", "EasyCpdlcVnsModule.cpp")));

            Assert.Equal(12, Regex.Matches(source, @"EASYCPDLC_DCDU_LSK_[LR][1-6]").Count);
            Assert.Contains("EASYCPDLC_DCDU_CONNECT", source);
            Assert.Contains("EASYCPDLC_DCDU_REPRINT", source);
            Assert.Contains("if (g_dcduMode)", source);
            Assert.Contains("if (!g_dcduMode && command >= 1 && command <= 18)", source);
            Assert.DoesNotContain("AS430_", source);
            Assert.DoesNotContain("EASYCPDLC_GNS_", source);
            Assert.DoesNotContain(">K:", source);
        }

        [Theory]
        [InlineData("123456", "secret", true)]
        [InlineData("", "", true)]
        [InlineData("ABC", "secret", false)]
        [InlineData("-1", "secret", false)]
        public void TrayCredentialEditor_ValidatesCid(string cid, string hoppie, bool expected)
        {
            Assert.Equal(expected, CredentialSettingsForm.TryValidate(cid, hoppie, out _, out _));
        }

        [Fact]
        public void MobiFlightCompanionProfile_UsesOnlyThePrivateModuleLVar()
        {
            string profile = File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "MobiFlight",
                "EasyCPDLC-VNS430-Module.mfproj"));

            using JsonDocument document = JsonDocument.Parse(profile);
            Assert.Equal("EasyCPDLC VNS430 Module", document.RootElement.GetProperty("Name").GetString());
            Assert.Equal(18, Regex.Matches(profile, @"\(>L:EASYCPDLC_VNS_COMMAND\)").Count);
            Assert.Equal(18, Regex.Matches(profile, @"MSFS2020CustomInputAction").Count);
            Assert.DoesNotContain("KeyInputAction", profile);
            Assert.DoesNotContain("AS430_", profile);
            Assert.DoesNotContain("GPS_", profile);
            Assert.DoesNotContain("EASYCPDLC_GNS_", profile);
            Assert.DoesNotContain(">K:", profile);
        }

        [Fact]
        public void RepositoryLayout_NestsVns430UnderPrintElcWithoutDuplicateGuides()
        {
            string repository = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", ".."));
            string vnsRoot = Path.Combine(repository, "EasyCPDLC", "VNS430");

            Assert.True(Directory.Exists(vnsRoot));
            Assert.True(Directory.Exists(Path.Combine(vnsRoot, "MSFS2024Module")));
            Assert.True(Directory.Exists(Path.Combine(vnsRoot, "Docs", "images")));
            Assert.False(Directory.Exists(Path.Combine(repository, "EasyCPDLC", "GNS430")));
            Assert.False(Directory.Exists(Path.Combine(vnsRoot, "Tutorial")));
            Assert.False(Directory.Exists(Path.Combine(vnsRoot, "Screenshots")));
            Assert.Equal(2, Directory.GetFiles(vnsRoot, "README.md", SearchOption.AllDirectories).Length);

            string packageBuilder = File.ReadAllText(Path.Combine(
                vnsRoot, "MSFS2024Module", "Build-Package.ps1"));
            Assert.Contains("Asobo_MPA_GNS430", packageBuilder);
            Assert.Contains("model/GNS430.xml", packageBuilder);
            Assert.Contains("EasyCPDLC/VNS430/VNS430.html", packageBuilder);
            Assert.DoesNotContain("Asobo_MPA_VNS430", packageBuilder);
        }

        [Fact]
        public void MobiFlightDcduProfile_ProvidesTwentyModeGatedPrivateInputs()
        {
            string profile = File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "MobiFlight",
                "EasyCPDLC-DCDU-Module.mfproj"));

            using JsonDocument document = JsonDocument.Parse(profile);
            Assert.Equal("EasyCPDLC DCDU Module", document.RootElement.GetProperty("Name").GetString());
            Assert.Equal(20, Regex.Matches(profile, @"\(>L:EASYCPDLC_DCDU_").Count);
            Assert.Equal(20, Regex.Matches(profile, @"MSFS2020CustomInputAction").Count);
            Assert.Equal(12, Regex.Matches(profile, @"EASYCPDLC_DCDU_LSK_[LR][1-6]").Count / 2);
            Assert.DoesNotContain("EASYCPDLC_VNS_COMMAND", profile);
            Assert.DoesNotContain("KeyInputAction", profile);
            Assert.DoesNotContain("AS430_", profile);
            Assert.DoesNotContain("GPS_", profile);
            Assert.DoesNotContain(">K:", profile);
        }

        [Theory]
        [InlineData(-1, 4, 3)]
        [InlineData(4, 4, 0)]
        [InlineData(7, 4, 3)]
        public void EncoderSelection_WrapsLikeThePhysicalUnit(int value, int count, int expected)
        {
            Assert.Equal(expected, Vns430Form.Wrap(value, count));
        }

        [Fact]
        public void MessageText_IsCollapsedAndWrappedForTheSmallDisplay()
        {
            Assert.Equal("CLIMB TO FL350 NOW", Vns430Form.CollapseWhitespace("  CLIMB\r\n TO   FL350 NOW "));
            Assert.Equal(new[] { "CLIMB TO", "FL350 NOW" }, Vns430Form.WrapText("CLIMB TO FL350 NOW", 9).ToArray());
        }

        [Theory]
        [InlineData((int)Vns430Page.Messages, true)]
        [InlineData((int)Vns430Page.MessageDetail, true)]
        [InlineData((int)Vns430Page.Status, false)]
        [InlineData((int)Vns430Page.Menu, false)]
        public void MessageSelection_IsPreservedOnlyAcrossListAndDetailPages(int page, bool expected)
        {
            Assert.Equal(expected, Vns430Form.ShouldPreserveMessageSelection((Vns430Page)page));
        }

        [Theory]
        [InlineData((int)Vns430PageGroup.Nav, (int)Vns430Page.Status, 2)]
        [InlineData((int)Vns430PageGroup.Wpt, (int)Vns430Page.Logon, 2)]
        [InlineData((int)Vns430PageGroup.Aux, (int)Vns430Page.AocMenu, 3)]
        [InlineData((int)Vns430PageGroup.Nrst, (int)Vns430Page.Messages, 1)]
        public void PageGroups_FollowThePhysicalLargeAndSmallKnobModel(int group, int firstPage, int count)
        {
            Vns430Page[] pages = Vns430Form.PagesForGroup((Vns430PageGroup)group);
            Assert.Equal(count, pages.Length);
            Assert.Equal((Vns430Page)firstPage, pages[0]);
        }

        [Fact]
        public void LcdRenderer_UsesTheNative240By128RasterAndSampledPalette()
        {
            Vns430LcdState state = new()
            {
                Snapshot = new Vns430BackendSnapshot
                {
                    Connected = true,
                    Callsign = "N731CD",
                    CurrentAtcUnit = "KZAK",
                    Messages = new List<Vns430MessageSnapshot>()
                },
                Page = Vns430Page.Status,
                PageGroup = Vns430PageGroup.Nav,
                CursorActive = true
            };

            // The palette is asserted on the raw raster. The older-LCD appearance pass
            // blends every pixel, so exact sampled colours only survive before it runs.
            using Bitmap image = Vns430LcdRenderer.Render(state, applyAppearance: false);
            Assert.Equal(240, image.Width);
            Assert.Equal(128, image.Height);
            HashSet<int> colors = new();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    colors.Add(image.GetPixel(x, y).ToArgb());
                }
            }
            Assert.Contains(Vns430LcdRenderer.Blue.ToArgb(), colors);
            Assert.Contains(Vns430LcdRenderer.Black.ToArgb(), colors);
            Assert.Contains(Vns430LcdRenderer.Cyan.ToArgb(), colors);
            Assert.Contains(Vns430LcdRenderer.Green.ToArgb(), colors);
        }

        [Fact]
        public void LcdRenderer_AppliesTheOlderDisplayAppearanceByDefault()
        {
            Vns430LcdState state = new()
            {
                Snapshot = new Vns430BackendSnapshot
                {
                    Connected = true,
                    Callsign = "N731CD",
                    CurrentAtcUnit = "KZAK",
                    Messages = new List<Vns430MessageSnapshot>()
                },
                Page = Vns430Page.Status,
                PageGroup = Vns430PageGroup.Nav,
                CursorActive = true
            };

            using Bitmap raw = Vns430LcdRenderer.Render(state, applyAppearance: false);
            using Bitmap shaded = Vns430LcdRenderer.Render(state);

            Assert.Equal(raw.Width, shaded.Width);
            Assert.Equal(raw.Height, shaded.Height);

            // Bloom and softening must visibly change the raster, and must widen the
            // palette rather than collapse it, so strokes read heavier without the
            // display losing legible contrast.
            int changed = 0;
            HashSet<int> rawColors = new();
            HashSet<int> shadedColors = new();
            for (int y = 0; y < raw.Height; y++)
            {
                for (int x = 0; x < raw.Width; x++)
                {
                    int before = raw.GetPixel(x, y).ToArgb();
                    int after = shaded.GetPixel(x, y).ToArgb();
                    rawColors.Add(before);
                    shadedColors.Add(after);
                    if (before != after)
                    {
                        changed++;
                    }
                }
            }

            Assert.True(changed > 0, "The appearance pass left the raster untouched.");
            Assert.True(shadedColors.Count > rawColors.Count,
                "The appearance pass did not introduce intermediate shades.");
        }

        [Fact]
        public void PhotoDerivedPanelArtwork_EmbedsBackgroundAndMovementStates()
        {
            using Vns430PanelArtwork artwork = new();
            Assert.Equal(960, artwork.PanelBackground.Width);
            Assert.Equal(407, artwork.PanelBackground.Height);

            using Bitmap surface = new(960, 407);
            using Graphics graphics = Graphics.FromImage(surface);
            artwork.DrawState(graphics, "enter", "pressed");
            artwork.DrawState(graphics, "right_encoder", "large-cw");
            artwork.DrawState(graphics, "right_encoder", "small-ccw");
        }

        [Fact]
        public void RangeRocker_UsesTwoGradientSegmentsWithOneSharedJoin()
        {
            RectangleF control = Vns430PanelArtwork.ControlBounds("range");
            RectangleF decrease = Vns430PanelArtwork.PressedSegmentBounds("range", "decrease-pressed", control);
            RectangleF increase = Vns430PanelArtwork.PressedSegmentBounds("range", "increase-pressed", control);

            Assert.Equal(decrease.Right, increase.Left);
            Assert.True(decrease.Left > control.Left);
            Assert.True(increase.Right < control.Right);
            Assert.Equal(decrease.Top, increase.Top);
            Assert.Equal(decrease.Height, increase.Height);
        }

        [Theory]
        [InlineData((int)Vns430PageGroup.Nav, "DLK")]
        [InlineData((int)Vns430PageGroup.Wpt, "ATC")]
        [InlineData((int)Vns430PageGroup.Aux, "AOC")]
        [InlineData((int)Vns430PageGroup.Nrst, "MSG")]
        public void PageGroupLabels_DescribeTheDatalinkFunctions(int group, string expected)
        {
            Assert.Equal(expected, Vns430LcdRenderer.PageGroupLabel((Vns430PageGroup)group));
        }

        [Fact]
        public void ComplexAtcWorkflow_BuildsTheBackendPacketTextFromRotaryFields()
        {
            Vns430BackendSnapshot snapshot = new() { CurrentAtcUnit = "KZAK" };
            Vns430Workflow workflow = Vns430Workflow.Create(Vns430WorkflowKind.AtcDirect, snapshot);
            workflow.Fields.Single(field => field.Key == "VALUE").Value = "DAG";
            workflow.Fields.Single(field => field.Key == "DUE").Value = "WX";
            workflow.Fields.Single(field => field.Key == "REMARKS").Value = "RIDE";

            Assert.Equal(string.Empty, workflow.ValidationError());
            Assert.Equal("REQUEST DIRECT TO DAG DUE TO WEATHER RIDE", workflow.BuildMessage(snapshot));
        }

        [Fact]
        public void AocAndLoadPages_RenderNativelyWithoutOpeningLegacyForms()
        {
            Vns430BackendSnapshot snapshot = new()
            {
                Connected = true,
                Callsign = "DAL123",
                Departure = "KSEA",
                Arrival = "KSFO",
                Aircraft = "B738"
            };
            foreach (Vns430Page page in new[]
            {
                Vns430Page.AtcMenu, Vns430Page.AtcRequest, Vns430Page.RequestReview,
                Vns430Page.AocMenu, Vns430Page.AocRequest, Vns430Page.AocReview,
                Vns430Page.LoadControl, Vns430Page.LoadReview
            })
            {
                Vns430Workflow workflow = page is Vns430Page.AtcRequest or Vns430Page.RequestReview
                    ? Vns430Workflow.Create(Vns430WorkflowKind.AtcLevel, snapshot)
                    : Vns430Workflow.Create(Vns430WorkflowKind.AocTelex, snapshot);
                using Bitmap image = Vns430LcdRenderer.Render(new Vns430LcdState
                {
                    Snapshot = snapshot,
                    Page = page,
                    PageGroup = page is Vns430Page.AtcMenu or Vns430Page.AtcRequest or Vns430Page.RequestReview
                        ? Vns430PageGroup.Wpt
                        : Vns430PageGroup.Aux,
                    Workflow = workflow,
                    CursorActive = true,
                    OperationStatus = "ENT LOAD SIMBRIEF"
                });
                Assert.Equal(240, image.Width);
                Assert.Equal(128, image.Height);
            }
        }

        [Theory]
        [InlineData("pressed")]
        [InlineData("decrease-pressed")]
        [InlineData("increase-pressed")]
        public void ButtonPresses_ReuseTheRegisteredNormalPhotoCrop(string state)
        {
            Assert.Equal("normal", Vns430PanelArtwork.AssetStateFor(state));
        }

        [Theory]
        [InlineData(0, 0, (int)Vns430Command.CursorPush)]
        [InlineData(-38, 0, (int)Vns430Command.None)]
        [InlineData(38, 0, (int)Vns430Command.None)]
        [InlineData(-63, 0, (int)Vns430Command.None)]
        [InlineData(63, 0, (int)Vns430Command.None)]
        [InlineData(80, 0, (int)Vns430Command.None)]
        public void LeftClick_OnlyPushesTheEncoderCenter(float offsetX, float offsetY, int expected)
        {
            PointF center = new(878, 326);
            PointF point = new(center.X + offsetX, center.Y + offsetY);

            Assert.Equal((Vns430Command)expected, Vns430Form.KnobPushCommandAt(point, center));
        }

        [Theory]
        [InlineData(0, 0, 120, (int)Vns430Command.SmallRightIncrease)]
        [InlineData(-38, 0, -120, (int)Vns430Command.SmallRightDecrease)]
        [InlineData(38, 0, 120, (int)Vns430Command.SmallRightIncrease)]
        [InlineData(-63, 0, -120, (int)Vns430Command.LargeRightDecrease)]
        [InlineData(63, 0, 120, (int)Vns430Command.LargeRightIncrease)]
        [InlineData(80, 0, 120, (int)Vns430Command.None)]
        [InlineData(38, 0, 0, (int)Vns430Command.None)]
        public void MouseWheel_RotatesTheRingUnderThePointer(float offsetX, float offsetY, int delta, int expected)
        {
            PointF center = new(878, 326);
            PointF point = new(center.X + offsetX, center.Y + offsetY);

            Assert.Equal((Vns430Command)expected, Vns430Form.KnobWheelCommandAt(point, center, delta));
        }

        [Fact]
        public void GnsPanel_DoesNotOverrideKeyboardCommandHandling()
        {
            Assert.Null(typeof(Vns430Form).GetMethod(
                "ProcessCmdKey",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly));
        }

        [Fact]
        public void EncoderSprites_MoveInnerAndOuterRingsIndependently()
        {
            using Vns430PanelArtwork artwork = new();
            Rectangle bounds = Rectangle.Round(Vns430PanelArtwork.ControlBounds("right_encoder"));
            using Bitmap normal = RenderPanelState(artwork, "right_encoder", "normal");
            using Bitmap small = RenderPanelState(artwork, "right_encoder", "small-cw");
            using Bitmap large = RenderPanelState(artwork, "right_encoder", "large-cw");

            PointF center = new(bounds.Left + (bounds.Width / 2f), bounds.Top + (bounds.Height / 2f));
            int smallInnerChanges = CountChangedPixels(normal, small, center, 0, 43);
            int smallOuterChanges = CountChangedPixels(normal, small, center, 52, 75);
            int largeInnerChanges = CountChangedPixels(normal, large, center, 0, 43);
            int largeOuterChanges = CountChangedPixels(normal, large, center, 52, 75);

            Assert.True(smallInnerChanges > smallOuterChanges, $"Small knob changed inner={smallInnerChanges}, outer={smallOuterChanges} pixels.");
            Assert.True(largeOuterChanges > largeInnerChanges, $"Large knob changed outer={largeOuterChanges}, inner={largeInnerChanges} pixels.");
        }

        [Theory]
        [InlineData("left_encoder", 78, 82)]
        [InlineData("right_encoder", 79, 83)]
        public void EncoderArtwork_UsesCalibratedMechanicalPivots(string control, float expectedX, float expectedY)
        {
            PointF pivot = Vns430PanelArtwork.EncoderPivot(control);

            Assert.Equal(expectedX, pivot.X);
            Assert.Equal(expectedY, pivot.Y);
        }

        private static Bitmap RenderPanelState(Vns430PanelArtwork artwork, string control, string state)
        {
            Bitmap surface = new(960, 407);
            using Graphics graphics = Graphics.FromImage(surface);
            graphics.DrawImageUnscaled(artwork.PanelBackground, 0, 0);
            artwork.DrawState(graphics, control, state);
            return surface;
        }

        private static int CountChangedPixels(Bitmap first, Bitmap second, PointF center, float minimumRadius, float maximumRadius)
        {
            int count = 0;
            int left = Math.Max(0, (int)(center.X - maximumRadius));
            int right = Math.Min(first.Width - 1, (int)(center.X + maximumRadius));
            int top = Math.Max(0, (int)(center.Y - maximumRadius));
            int bottom = Math.Min(first.Height - 1, (int)(center.Y + maximumRadius));
            float minimumSquared = minimumRadius * minimumRadius;
            float maximumSquared = maximumRadius * maximumRadius;

            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    float dx = x - center.X;
                    float dy = y - center.Y;
                    float distanceSquared = (dx * dx) + (dy * dy);
                    if (distanceSquared >= minimumSquared && distanceSquared <= maximumSquared && first.GetPixel(x, y) != second.GetPixel(x, y))
                    {
                        count += 1;
                    }
                }
            }

            return count;
        }
    }
}
