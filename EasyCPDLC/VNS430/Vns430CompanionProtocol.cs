using System;
using System.Runtime.InteropServices;

namespace EasyCPDLC.VNS430
{
    internal static class Vns430CompanionProtocol
    {
        internal const uint Magic = 0x45435044;
        internal const uint Version = 1;
        internal const string CommandClientDataName = "EasyCPDLC.VNS430.Command.v1";
        internal const string StatusClientDataName = "EasyCPDLC.VNS430.Status.v1";

        internal const string CommandLVar = "EASYCPDLC_VNS_COMMAND";
        internal const string ModuleAliveLVar = "EASYCPDLC_VNS_MODULE_ALIVE";
        internal const string AppConnectedLVar = "EASYCPDLC_VNS_APP_CONNECTED";
        internal const string VatsimConnectedLVar = "EASYCPDLC_VNS_VATSIM_CONNECTED";
        internal const string UnreadCountLVar = "EASYCPDLC_VNS_UNREAD_COUNT";
        internal const string PageLVar = "EASYCPDLC_VNS_PAGE";
        internal const string CursorActiveLVar = "EASYCPDLC_VNS_CURSOR_ACTIVE";
        internal const string DcduModeLVar = "EASYCPDLC_DCDU_MODE";

        internal const uint StatusAppOnline = 1 << 0;
        internal const uint StatusVatsimConnected = 1 << 1;
        internal const uint StatusCursorActive = 1 << 2;
        internal const uint StatusDcduMode = 1 << 3;
        internal const uint ChecksumSeed = 0x430C0DEC;

        internal static uint CalculateCommandChecksum(uint sequence, uint command)
        {
            return ChecksumSeed ^ Magic ^ Version ^ sequence ^ command;
        }

        internal static bool TryReadCommand(Vns430CompanionCommandPacket packet, out Vns430Command command)
        {
            command = Vns430Command.None;
            if (packet.Magic != Magic ||
                packet.Version != Version ||
                packet.Checksum != CalculateCommandChecksum(packet.Sequence, packet.Command) ||
                packet.Command > byte.MaxValue ||
                !Enum.IsDefined(typeof(Vns430Command), (byte)packet.Command))
            {
                return false;
            }

            command = (Vns430Command)(byte)packet.Command;
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Vns430CompanionCommandPacket
    {
        internal uint Magic;
        internal uint Version;
        internal uint Sequence;
        internal uint Command;
        internal uint Checksum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Vns430CompanionStatusPacket
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
