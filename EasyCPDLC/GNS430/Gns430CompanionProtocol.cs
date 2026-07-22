using System;
using System.Runtime.InteropServices;

namespace EasyCPDLC.GNS430
{
    internal static class Gns430CompanionProtocol
    {
        internal const uint Magic = 0x45435044;
        internal const uint Version = 1;
        internal const string CommandClientDataName = "EasyCPDLC.GNS430.Command.v1";
        internal const string StatusClientDataName = "EasyCPDLC.GNS430.Status.v1";

        internal const string CommandLVar = "EASYCPDLC_GNS_COMMAND";
        internal const string ModuleAliveLVar = "EASYCPDLC_GNS_MODULE_ALIVE";
        internal const string AppConnectedLVar = "EASYCPDLC_GNS_APP_CONNECTED";
        internal const string VatsimConnectedLVar = "EASYCPDLC_GNS_VATSIM_CONNECTED";
        internal const string UnreadCountLVar = "EASYCPDLC_GNS_UNREAD_COUNT";
        internal const string PageLVar = "EASYCPDLC_GNS_PAGE";
        internal const string CursorActiveLVar = "EASYCPDLC_GNS_CURSOR_ACTIVE";

        internal const uint StatusAppOnline = 1 << 0;
        internal const uint StatusVatsimConnected = 1 << 1;
        internal const uint StatusCursorActive = 1 << 2;
        internal const uint ChecksumSeed = 0x430C0DEC;

        internal static uint CalculateCommandChecksum(uint sequence, uint command)
        {
            return ChecksumSeed ^ Magic ^ Version ^ sequence ^ command;
        }

        internal static bool TryReadCommand(Gns430CompanionCommandPacket packet, out Gns430Command command)
        {
            command = Gns430Command.None;
            if (packet.Magic != Magic ||
                packet.Version != Version ||
                packet.Checksum != CalculateCommandChecksum(packet.Sequence, packet.Command) ||
                packet.Command > byte.MaxValue ||
                !Enum.IsDefined(typeof(Gns430Command), (byte)packet.Command))
            {
                return false;
            }

            command = (Gns430Command)(byte)packet.Command;
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Gns430CompanionCommandPacket
    {
        internal uint Magic;
        internal uint Version;
        internal uint Sequence;
        internal uint Command;
        internal uint Checksum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Gns430CompanionStatusPacket
    {
        internal uint Magic;
        internal uint Version;
        internal uint Sequence;
        internal uint Flags;
        internal uint UnreadCount;
        internal uint Page;
        internal uint Reserved0;
        internal uint Reserved1;
    }
}
