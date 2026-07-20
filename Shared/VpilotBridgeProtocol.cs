using System;
using System.Globalization;
using System.Text;

namespace EasyCPDLC.VPilotBridge.Protocol
{
    public enum VpilotBridgePacketKind
    {
        Unknown,
        PrivateMessage,
        NetworkConnected,
        NetworkDisconnected,
        SendPrivateMessage,
        Result
    }

    public sealed class VpilotBridgePacket
    {
        public VpilotBridgePacketKind Kind { get; set; }
        public string Id { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Callsign { get; set; } = string.Empty;
        public string Peer { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public static class VpilotBridgeProtocol
    {
        public const string PipeName = "EasyCPDLC.VPilotBridge.v1";
        private const string Version = "V1";
        private const int MaximumLineLength = 65536;

        public static string Encode(VpilotBridgePacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            string id = string.IsNullOrWhiteSpace(packet.Id) ? Guid.NewGuid().ToString("N") : packet.Id.Trim();
            DateTime timestamp = packet.TimestampUtc.Kind == DateTimeKind.Utc
                ? packet.TimestampUtc
                : packet.TimestampUtc.ToUniversalTime();

            return string.Join("|",
                Version,
                KindToken(packet.Kind),
                id,
                timestamp.Ticks.ToString(CultureInfo.InvariantCulture),
                ToBase64(packet.Callsign),
                ToBase64(packet.Peer),
                ToBase64(packet.Message));
        }

        public static bool TryDecode(string line, out VpilotBridgePacket packet)
        {
            packet = null;
            if (string.IsNullOrWhiteSpace(line) || line.Length > MaximumLineLength)
            {
                return false;
            }

            string[] fields = line.Split('|');
            if (fields.Length != 7 || !string.Equals(fields[0], Version, StringComparison.Ordinal))
            {
                return false;
            }

            VpilotBridgePacketKind kind = ParseKind(fields[1]);
            if (kind == VpilotBridgePacketKind.Unknown ||
                string.IsNullOrWhiteSpace(fields[2]) ||
                !long.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
            {
                return false;
            }

            try
            {
                packet = new VpilotBridgePacket
                {
                    Kind = kind,
                    Id = fields[2].Trim(),
                    TimestampUtc = new DateTime(ticks, DateTimeKind.Utc),
                    Callsign = FromBase64(fields[4]),
                    Peer = FromBase64(fields[5]),
                    Message = FromBase64(fields[6])
                };
                return true;
            }
            catch (Exception)
            {
                packet = null;
                return false;
            }
        }

        private static string KindToken(VpilotBridgePacketKind kind)
        {
            switch (kind)
            {
                case VpilotBridgePacketKind.PrivateMessage: return "PRIVATE";
                case VpilotBridgePacketKind.NetworkConnected: return "CONNECTED";
                case VpilotBridgePacketKind.NetworkDisconnected: return "DISCONNECTED";
                case VpilotBridgePacketKind.SendPrivateMessage: return "SEND_PRIVATE";
                case VpilotBridgePacketKind.Result: return "RESULT";
                default: return "UNKNOWN";
            }
        }

        private static VpilotBridgePacketKind ParseKind(string token)
        {
            switch ((token ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "PRIVATE": return VpilotBridgePacketKind.PrivateMessage;
                case "CONNECTED": return VpilotBridgePacketKind.NetworkConnected;
                case "DISCONNECTED": return VpilotBridgePacketKind.NetworkDisconnected;
                case "SEND_PRIVATE": return VpilotBridgePacketKind.SendPrivateMessage;
                case "RESULT": return VpilotBridgePacketKind.Result;
                default: return VpilotBridgePacketKind.Unknown;
            }
        }

        private static string ToBase64(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string FromBase64(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
        }
    }
}
