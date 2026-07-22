using EasyCPDLC.GNS430;
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
    public sealed class Gns430Tests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(18)]
        public void CompanionPackets_ValidateEveryDefinedCommand(byte value)
        {
            Gns430CompanionCommandPacket packet = new()
            {
                Magic = Gns430CompanionProtocol.Magic,
                Version = Gns430CompanionProtocol.Version,
                Sequence = 27,
                Command = value,
                Checksum = Gns430CompanionProtocol.CalculateCommandChecksum(27, value)
            };

            Assert.True(Gns430CompanionProtocol.TryReadCommand(packet, out Gns430Command command));
            Assert.Equal((Gns430Command)value, command);
        }

        [Fact]
        public void CompanionPackets_RejectDamageAndUnknownCommands()
        {
            Gns430CompanionCommandPacket packet = new()
            {
                Magic = Gns430CompanionProtocol.Magic,
                Version = Gns430CompanionProtocol.Version,
                Sequence = 5,
                Command = 99,
                Checksum = Gns430CompanionProtocol.CalculateCommandChecksum(5, 99)
            };

            Assert.False(Gns430CompanionProtocol.TryReadCommand(packet, out _));

            packet.Command = (uint)Gns430Command.Menu;
            packet.Checksum = 0;
            Assert.False(Gns430CompanionProtocol.TryReadCommand(packet, out _));
        }

        [Fact]
        public void CompanionProtocol_HasStableCrossLanguageLayoutAndPrivateNames()
        {
            Assert.Equal(20, Marshal.SizeOf<Gns430CompanionCommandPacket>());
            Assert.Equal(32, Marshal.SizeOf<Gns430CompanionStatusPacket>());
            Assert.StartsWith("EasyCPDLC.", Gns430CompanionProtocol.CommandClientDataName);
            Assert.StartsWith("EASYCPDLC_", Gns430CompanionProtocol.CommandLVar);
            Assert.DoesNotContain("GPS_", Gns430CompanionProtocol.CommandClientDataName);
            Assert.DoesNotContain("AS430", Gns430CompanionProtocol.CommandClientDataName);
        }

        [Fact]
        public void MobiFlightCompanionProfile_UsesOnlyThePrivateModuleLVar()
        {
            string profile = File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "MobiFlight",
                "EasyCPDLC-GNS430-Companion.mfproj"));

            using JsonDocument document = JsonDocument.Parse(profile);
            Assert.Equal("EasyCPDLC GNS 430 Companion Module", document.RootElement.GetProperty("Name").GetString());
            Assert.Equal(18, Regex.Matches(profile, @"\(>L:EASYCPDLC_GNS_COMMAND\)").Count);
            Assert.Equal(18, Regex.Matches(profile, @"MSFS2020CustomInputAction").Count);
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
            Assert.Equal(expected, Gns430Form.Wrap(value, count));
        }

        [Fact]
        public void MessageText_IsCollapsedAndWrappedForTheSmallDisplay()
        {
            Assert.Equal("CLIMB TO FL350 NOW", Gns430Form.CollapseWhitespace("  CLIMB\r\n TO   FL350 NOW "));
            Assert.Equal(new[] { "CLIMB TO", "FL350 NOW" }, Gns430Form.WrapText("CLIMB TO FL350 NOW", 9).ToArray());
        }

        [Theory]
        [InlineData((int)Gns430Page.Messages, true)]
        [InlineData((int)Gns430Page.MessageDetail, true)]
        [InlineData((int)Gns430Page.Status, false)]
        [InlineData((int)Gns430Page.Menu, false)]
        public void MessageSelection_IsPreservedOnlyAcrossListAndDetailPages(int page, bool expected)
        {
            Assert.Equal(expected, Gns430Form.ShouldPreserveMessageSelection((Gns430Page)page));
        }

        [Theory]
        [InlineData((int)Gns430PageGroup.Nav, (int)Gns430Page.Status, 2)]
        [InlineData((int)Gns430PageGroup.Wpt, (int)Gns430Page.Logon, 1)]
        [InlineData((int)Gns430PageGroup.Aux, (int)Gns430Page.Help, 1)]
        [InlineData((int)Gns430PageGroup.Nrst, (int)Gns430Page.Messages, 1)]
        public void PageGroups_FollowThePhysicalLargeAndSmallKnobModel(int group, int firstPage, int count)
        {
            Gns430Page[] pages = Gns430Form.PagesForGroup((Gns430PageGroup)group);
            Assert.Equal(count, pages.Length);
            Assert.Equal((Gns430Page)firstPage, pages[0]);
        }

        [Fact]
        public void LcdRenderer_UsesTheNative240By128RasterAndSampledPalette()
        {
            Gns430LcdState state = new()
            {
                Snapshot = new Gns430BackendSnapshot
                {
                    Connected = true,
                    Callsign = "N731CD",
                    CurrentAtcUnit = "KZAK",
                    Messages = new List<Gns430MessageSnapshot>()
                },
                Page = Gns430Page.Status,
                PageGroup = Gns430PageGroup.Nav,
                CursorActive = true
            };

            using Bitmap image = Gns430LcdRenderer.Render(state);
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
            Assert.Contains(Gns430LcdRenderer.Blue.ToArgb(), colors);
            Assert.Contains(Gns430LcdRenderer.Black.ToArgb(), colors);
            Assert.Contains(Gns430LcdRenderer.Cyan.ToArgb(), colors);
            Assert.Contains(Gns430LcdRenderer.Green.ToArgb(), colors);
        }

        [Fact]
        public void PhotoDerivedPanelArtwork_EmbedsBackgroundAndMovementStates()
        {
            using Gns430PanelArtwork artwork = new();
            Assert.Equal(960, artwork.PanelBackground.Width);
            Assert.Equal(407, artwork.PanelBackground.Height);

            using Bitmap surface = new(960, 407);
            using Graphics graphics = Graphics.FromImage(surface);
            artwork.DrawState(graphics, "enter", "pressed");
            artwork.DrawState(graphics, "right_encoder", "large-cw");
            artwork.DrawState(graphics, "right_encoder", "small-ccw");
        }
    }
}
