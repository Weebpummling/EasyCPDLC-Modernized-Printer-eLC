using System.Collections.Generic;

namespace EasyCPDLC.GNS430
{
    internal enum Gns430PageGroup
    {
        Nav,
        Wpt,
        Aux,
        Nrst
    }

    internal enum Gns430Page
    {
        Status,
        Messages,
        MessageDetail,
        Logon,
        Menu,
        Help
    }

    internal sealed class Gns430MessageSnapshot
    {
        internal CPDLCMessage Source { get; init; }
        internal string Type { get; init; } = string.Empty;
        internal string Station { get; init; } = string.Empty;
        internal string Text { get; init; } = string.Empty;
        internal bool Outbound { get; init; }
        internal bool Acknowledged { get; init; }
        internal bool Unread { get; init; }
        internal IReadOnlyList<string> Responses { get; init; } = new string[0];
    }

    internal sealed class Gns430BackendSnapshot
    {
        internal bool Connected { get; init; }
        internal string Callsign { get; init; } = string.Empty;
        internal string CurrentAtcUnit { get; init; } = string.Empty;
        internal string PendingLogon { get; init; } = string.Empty;
        internal IReadOnlyList<Gns430MessageSnapshot> Messages { get; init; } = new Gns430MessageSnapshot[0];
    }
}
