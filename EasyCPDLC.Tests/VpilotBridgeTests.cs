using EasyCPDLC.VPilotBridge.Protocol;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyCPDLC.Tests
{
    public sealed class VpilotBridgeTests
    {
        [Fact]
        public void Protocol_RoundTripsMultilineUnicodeWithoutCredentials()
        {
            VpilotBridgePacket source = new()
            {
                Kind = VpilotBridgePacketKind.PrivateMessage,
                Id = "event-42",
                TimestampUtc = new DateTime(2026, 7, 20, 18, 42, 0, DateTimeKind.Utc),
                Callsign = "CI7752",
                Peer = "ACARS",
                Message = "PDC CLEARED TO KSNA\nSQUAWK 4271 – EXPECT SID"
            };

            string encoded = VpilotBridgeProtocol.Encode(source);

            Assert.True(VpilotBridgeProtocol.TryDecode(encoded, out VpilotBridgePacket decoded));
            Assert.Equal(source.Kind, decoded.Kind);
            Assert.Equal(source.Id, decoded.Id);
            Assert.Equal(source.TimestampUtc, decoded.TimestampUtc);
            Assert.Equal(source.Callsign, decoded.Callsign);
            Assert.Equal(source.Peer, decoded.Peer);
            Assert.Equal(source.Message, decoded.Message);
            Assert.DoesNotContain("PDC CLEARED", encoded);
        }

        [Fact]
        public void Protocol_RoundTripsStructuredContactMePacket()
        {
            VpilotBridgePacket source = new()
            {
                Kind = VpilotBridgePacketKind.ContactMe,
                Id = "contact-42",
                TimestampUtc = new DateTime(2026, 7, 22, 19, 15, 0, DateTimeKind.Utc),
                Callsign = "CI7752",
                Peer = "SFO_APP",
                Message = "Please contact me on 124.525",
                Facility = "SFO APPROACH",
                Frequency = "124.525"
            };

            string encoded = VpilotBridgeProtocol.Encode(source);

            Assert.StartsWith("V2|CONTACT_ME|", encoded);
            Assert.True(VpilotBridgeProtocol.TryDecode(encoded, out VpilotBridgePacket decoded));
            Assert.Equal(source.Kind, decoded.Kind);
            Assert.Equal(source.Peer, decoded.Peer);
            Assert.Equal(source.Facility, decoded.Facility);
            Assert.Equal(source.Frequency, decoded.Frequency);
            Assert.Equal(source.Message, decoded.Message);
        }

        [Fact]
        public void Protocol_StillDecodesVersionOnePackets()
        {
            string legacy = "V1|PRIVATE|legacy-1|1|Q0kxMjM=|QUNBUlM=|UERDIENMRUFSRUQ=";

            Assert.True(VpilotBridgeProtocol.TryDecode(legacy, out VpilotBridgePacket decoded));
            Assert.Equal(VpilotBridgePacketKind.PrivateMessage, decoded.Kind);
            Assert.Equal("CI123", decoded.Callsign);
            Assert.Equal("ACARS", decoded.Peer);
            Assert.Equal("PDC CLEARED", decoded.Message);
            Assert.Equal(string.Empty, decoded.Frequency);
        }

        [Theory]
        [InlineData("SFO_APP", "Please contact me on 124.525", 0, "SFO APPROACH", "124.525")]
        [InlineData("LON_CTR", "CONTACT ME 133,6", 0, "LON CENTER", "133.600")]
        [InlineData("KSEA_TWR", "Please contact me", 19300, "KSEA TOWER", "119.300")]
        [InlineData("EDDM_GND", "Contact EDDM_GND on 121700", 0, "EDDM GROUND", "121.700")]
        public void ContactParser_ExtractsControllerFacilityAndFrequency(
            string sender,
            string message,
            int fallback,
            string expectedFacility,
            string expectedFrequency)
        {
            Assert.True(VpilotContactMeParser.TryParse(sender, message, fallback, out VpilotContactDetails details));
            Assert.Equal(sender, details.ControllerCallsign);
            Assert.Equal(expectedFacility, details.Facility);
            Assert.Equal(expectedFrequency, details.Frequency);
        }

        [Theory]
        [InlineData("FRIEND1", "Please contact me on 124.525")]
        [InlineData("KSFO_DEL", "PDC CLEARED TO KSNA SQUAWK 4271")]
        [InlineData("KSFO_APP", "Please contact me later")]
        [InlineData("KSFO_APP", "Please contact me on 146.520")]
        public void ContactParser_IgnoresNonAtcOrIncompleteMessages(string sender, string message)
        {
            Assert.False(VpilotContactMeParser.TryParse(sender, message, 0, out _));
        }

        [Fact]
        public void ContactFormatter_CreatesReviewablePrintableMessage()
        {
            VpilotContactDetails details = new()
            {
                ControllerCallsign = "SFO_APP",
                Facility = "SFO APPROACH",
                Frequency = "124.525",
                OriginalMessage = "Please contact me on 124.525"
            };

            string result = VpilotContactMeParser.FormatDisplayMessage(details);

            Assert.Equal(
                "ATC CONTACT REQUEST\nFACILITY: SFO APPROACH\nCALLSIGN: SFO_APP\nFREQUENCY: 124.525\nMESSAGE: PLEASE CONTACT ME ON 124.525",
                result);

            CPDLCMessage message = new("INFO", "SFO_APP", result)
            {
                transport = "VATSIM/VPILOT",
                aircraftCallsign = "CI7752"
            };
            Assert.True(DatalinkPrinter.IsPrintableMessage(message));
        }

        [Fact]
        public void ContactDuplicateSuppression_BlocksReconnectReplayButAllowsLaterReminder()
        {
            DateTime firstSeen = new(2026, 7, 22, 19, 15, 0, DateTimeKind.Utc);
            DatalinkPrintDeduplicator deduplicator = new(TimeSpan.FromMinutes(15));
            string stableId = DatalinkPrinter.DeriveStableMessageId(
                "ATC CONTACT",
                "SFO_APP",
                "CI7752",
                "FREQUENCY: 124.525");

            Assert.True(deduplicator.TryRegister(stableId, firstSeen));
            Assert.False(deduplicator.TryRegister(stableId, firstSeen.AddSeconds(10)));
            Assert.True(deduplicator.TryRegister(stableId, firstSeen.AddMinutes(16)));
        }

        [Theory]
        [InlineData("ACARS", "PDC - CLEARED TO KSNA VIA SUSEY3 DEPARTURE", true)]
        [InlineData("ACARS", "CLRD TO KSNA SQUAWK 4271", true)]
        [InlineData("KSFO_DEL", "PDC CLEARANCE SQUAWK 4271", true)]
        [InlineData("KSFO_DEL", "PLEASE CONTACT ME ON 121.8", false)]
        [InlineData("FRIEND1", "HELLO FROM VATSIM", false)]
        public void Classifier_ImportsOnlyPdcMessages(string sender, string message, bool expected)
        {
            Assert.Equal(expected, VpilotPdcClassifier.IsPdc(sender, message));
        }

        [Fact]
        public async Task Client_ReceivesAndSendsPacketsOverCurrentUserPipe()
        {
            using NamedPipeServerStream server = new(
                VpilotBridgeProtocol.PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            using VpilotBridgeClient client = new();
            TaskCompletionSource<VpilotBridgePacket> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
            client.PacketReceived += (_, packet) => received.TrySetResult(packet);

            Task waiting = server.WaitForConnectionAsync();
            client.Start();
            await waiting.WaitAsync(TimeSpan.FromSeconds(5));

            using StreamReader reader = new(server, new UTF8Encoding(false), false, 4096, true);
            using StreamWriter writer = new(server, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
            VpilotBridgePacket inbound = new()
            {
                Kind = VpilotBridgePacketKind.PrivateMessage,
                Id = "inbound-1",
                TimestampUtc = DateTime.UtcNow,
                Callsign = "CI7752",
                Peer = "ACARS",
                Message = "PDC CLEARED TO KSNA SQUAWK 4271"
            };
            await writer.WriteLineAsync(VpilotBridgeProtocol.Encode(inbound));

            VpilotBridgePacket delivered = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(inbound.Id, delivered.Id);
            Assert.True(client.TrySendPrivateMessage("KSFO_DEL", "TEST MESSAGE"));

            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
            string commandLine = await reader.ReadLineAsync(timeout.Token);
            Assert.True(VpilotBridgeProtocol.TryDecode(commandLine, out VpilotBridgePacket command));
            Assert.Equal(VpilotBridgePacketKind.SendPrivateMessage, command.Kind);
            Assert.Equal("KSFO_DEL", command.Peer);
            Assert.Equal("TEST MESSAGE", command.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("V1|PRIVATE|too-few")]
        [InlineData("V2|PRIVATE|id|1|||")]
        [InlineData("V1|UNKNOWN|id|1|||")]
        public void Protocol_RejectsMalformedInput(string value)
        {
            Assert.False(VpilotBridgeProtocol.TryDecode(value, out _));
        }
    }
}
