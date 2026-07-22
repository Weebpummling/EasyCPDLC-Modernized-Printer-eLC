using EasyCPDLC.VPilotBridge.Protocol;
using RossCarlson.Vatsim.Vpilot.Plugins;
using RossCarlson.Vatsim.Vpilot.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EasyCPDLC.VPilotBridge
{
    public sealed class Plugin : IPlugin
    {
        private IBroker broker;
        private VpilotPipeServer pipeServer;
        private SynchronizationContext vpilotContext;
        private string connectedCallsign = string.Empty;
        private readonly Dictionary<string, int> controllerFrequencies =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public string Name => "EasyCPDLC vPilot Bridge";

        public void Initialize(IBroker pluginBroker)
        {
            broker = pluginBroker ?? throw new ArgumentNullException(nameof(pluginBroker));
            vpilotContext = SynchronizationContext.Current;
            pipeServer = new VpilotPipeServer(HandleBridgeCommand);
            pipeServer.Start();

            broker.NetworkConnected += NetworkConnected;
            broker.NetworkDisconnected += NetworkDisconnected;
            broker.PrivateMessageReceived += PrivateMessageReceived;
            broker.ControllerAdded += ControllerAdded;
            broker.ControllerFrequencyChanged += ControllerFrequencyChanged;
            broker.ControllerDeleted += ControllerDeleted;
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
            string senderCallsign = (e.From ?? string.Empty).Trim().ToUpperInvariant();
            int fallbackFrequency = 0;
            controllerFrequencies.TryGetValue(senderCallsign, out fallbackFrequency);

            if (VpilotContactMeParser.TryParse(senderCallsign, e.Message, fallbackFrequency, out VpilotContactDetails contact))
            {
                pipeServer.Publish(new VpilotBridgePacket
                {
                    Kind = VpilotBridgePacketKind.ContactMe,
                    Id = Guid.NewGuid().ToString("N"),
                    TimestampUtc = DateTime.UtcNow,
                    Callsign = connectedCallsign,
                    Peer = contact.ControllerCallsign,
                    Message = contact.OriginalMessage,
                    Facility = contact.Facility,
                    Frequency = contact.Frequency
                });
                return;
            }

            pipeServer.Publish(new VpilotBridgePacket
            {
                Kind = VpilotBridgePacketKind.PrivateMessage,
                Id = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
                Callsign = connectedCallsign,
                Peer = senderCallsign,
                Message = e.Message ?? string.Empty
            });
        }

        private void ControllerAdded(object sender, ControllerAddedEventArgs e)
        {
            string callsign = (e.Callsign ?? string.Empty).Trim().ToUpperInvariant();
            if (callsign.Length > 0)
            {
                controllerFrequencies[callsign] = e.Frequency;
            }
        }

        private void ControllerFrequencyChanged(object sender, ControllerFrequencyChangedEventArgs e)
        {
            string callsign = (e.Callsign ?? string.Empty).Trim().ToUpperInvariant();
            if (callsign.Length > 0)
            {
                controllerFrequencies[callsign] = e.NewFrequency;
            }
        }

        private void ControllerDeleted(object sender, ControllerDeletedEventArgs e)
        {
            controllerFrequencies.Remove((e.Callsign ?? string.Empty).Trim());
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
            controllerFrequencies.Clear();
            pipeServer?.Dispose();
            pipeServer = null;
        }
    }
}
