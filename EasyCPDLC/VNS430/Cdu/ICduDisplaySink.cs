using System.Collections.Generic;

namespace EasyCPDLC.VNS430.Cdu
{
    // Seam for streaming the CDU screen off-box. The grid is already serialised to the
    // WinWing CDU display format (28x12 = 336 [glyph, colour, size, inverse] cells) by
    // CduGrid.ToWinwingData(); a sink just forwards that payload somewhere. The renderer
    // pushes to its sink after every paint, so what is on screen is what a hardware CDU
    // would show.
    //
    // The "standby" WinWing export mod implements this by opening a websocket to
    // ws://localhost:8320/winwing/cdu-captain and sending { "Target": "Display", "Data": <cells> }.
    // Until then the default NullCduDisplaySink discards the payload.
    internal interface ICduDisplaySink
    {
        void Push(IReadOnlyList<object[]> cells);
    }

    internal sealed class NullCduDisplaySink : ICduDisplaySink
    {
        public static readonly NullCduDisplaySink Instance = new();

        public void Push(IReadOnlyList<object[]> cells)
        {
            // Discarded until a real WinWing/websocket sink is wired in.
        }
    }
}
