using EasyCPDLC.VPilotBridge.Protocol;
using RossCarlson.Vatsim.Vpilot.Plugins;
using RossCarlson.Vatsim.Vpilot.Plugins.Events;
using System;
using System.Threading;

namespace EasyCPDLC.VPilotBridge
{
    public sealed class Plugin : IPlugin
    {
        private IBroker broker;
        private VpilotPipeServer pipeServer;
        private SynchronizationContext vpilotContext;
        private string connectedCallsign = string.Empty;

        public string Name => "EasyCPDLC vPilot PDC Bridge";

        public void Initialize(IBroker pluginBroker)
        {
            broker = pluginBroker ?? throw new ArgumentNullException(nameof(pluginBroker));
            vpilotContext = SynchronizationContext.Current;
            pipeServer = new VpilotPipeServer(HandleBridgeCommand);
            pipeServer.Start();

            broker.NetworkConnected += NetworkConnected;
            broker.NetworkDisconnected += NetworkDisconnected;
            broker.PrivateMessageReceived += PrivateMessageReceived;
            broker.SessionEnded += SessionEnded;
            broker.PostDebugMessage(Name + " loaded. Waiting for EasyCPDLC on the current-user pipe.");
        }

        private void NetworkConnected(object sender, NetworkConnectedEventArgs e)
        {
            connectedCallsign = (e.Callsign ?? string.Empty).Trim().ToUpperInvariant();
            pipeServer.Publish(new VpilotBridgePacket
            {
                Kind = VpilotBridgePacketKind.NetworkConnected,
                Id = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                Callsign = connectedCallsign
            });
        }

        private void NetworkDisconnected(object sender, EventArgs e)
        {
            string previousCallsign = connectedCallsign;
            connectedCallsign = string.Empty;
            pipeServer.Publish(new VpilotBridgePacket
            {
                Kind = VpilotBridgePacketKind.NetworkDisconnected,
                Id = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                Callsign = previousCallsign
            });
        }

        private void PrivateMessageReceived(object sender, PrivateMessageReceivedEventArgs e)
        {
            pipeServer.Publish(new VpilotBridgePacket
            {
                Kind = VpilotBridgePacketKind.PrivateMessage,
                Id = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                Callsign = connectedCallsign,
                Peer = e.From ?? string.Empty,
                Message = e.Message ?? string.Empty
            });
        }

        private void HandleBridgeCommand(VpilotBridgePacket packet)
        {
            if (packet == null || packet.Kind != VpilotBridgePacketKind.SendPrivateMessage)
            {
                return;
            }

            Action send = () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(packet.Peer) || string.IsNullOrWhiteSpace(packet.Message))
                    {
                        PublishResult(packet.Id, "ERROR", "Recipient and message are required.");
                        return;
                    }

                    broker.SendPrivateMessage(packet.Peer.Trim(), packet.Message);
                    PublishResult(packet.Id, "OK", "Private message submitted through vPilot.");
                }
                catch (Exception ex)
                {
                    PublishResult(packet.Id, "ERROR", ex.Message);
                }
            };

            if (vpilotContext != null)
            {
                vpilotContext.Post(_ => send(), null);
            }
            else
            {
                send();
            }
        }

        private void PublishResult(string requestId, string status, string message)
        {
            pipeServer.Publish(new VpilotBridgePacket
            {
                Kind = VpilotBridgePacketKind.Result,
                Id = string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId,
                TimestampUtc = DateTime.UtcNow,
                Callsign = connectedCallsign,
                Peer = status,
                Message = message ?? string.Empty
            });
        }

        private void SessionEnded(object sender, EventArgs e)
        {
            pipeServer?.Dispose();
            pipeServer = null;
        }
    }
}
