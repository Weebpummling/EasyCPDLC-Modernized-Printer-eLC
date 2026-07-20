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
