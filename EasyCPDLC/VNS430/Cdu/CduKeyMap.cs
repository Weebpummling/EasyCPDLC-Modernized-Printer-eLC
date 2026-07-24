using System.Collections.Generic;

namespace EasyCPDLC.VNS430.Cdu
{
    internal enum CduKeyKind
    {
        Function,
        Alpha,
        Digit,
        Symbol
    }

    internal sealed class CduKey
    {
        public string Legend { get; init; } = string.Empty;
        public Vns430Command Command { get; init; }
        public CduKeyKind Kind { get; init; }

        // Logical position in the keypad grid (below the screen). The on-screen renderer,
        // the click zones, the bridge L-vars and the MobiFlight profile all derive from this.
        public int Row { get; init; }
        public int Col { get; init; }
    }

    // Single source of truth for the Boeing CDU (PFP/FMC) keypad. Art is applied on top of
    // these zones later; nothing here depends on any artwork.
    internal static class CduKeyMap
    {
        public static readonly IReadOnlyList<CduKey> Keys = Build();

        private static IReadOnlyList<CduKey> Build()
        {
            List<CduKey> keys = new();

            void Fn(string legend, Vns430Command cmd, int row, int col) =>
                keys.Add(new CduKey { Legend = legend, Command = cmd, Kind = CduKeyKind.Function, Row = row, Col = col });

            // Function block (rows 0..2, six columns).
            Fn("INIT\nREF", Vns430Command.CduInitRef, 0, 0);
            Fn("RTE", Vns430Command.CduRoute, 0, 1);
            Fn("DEP\nARR", Vns430Command.CduDepArr, 0, 2);
            Fn("ATC", Vns430Command.CduAtcPage, 0, 3);
            Fn("VNAV", Vns430Command.CduVnav, 0, 4);
            Fn("FIX", Vns430Command.CduFix, 0, 5);
            Fn("LEGS", Vns430Command.CduLegs, 1, 0);
            Fn("HOLD", Vns430Command.CduHold, 1, 1);
            Fn("FMC\nCOMM", Vns430Command.CduFmcComm, 1, 2);
            Fn("PROG", Vns430Command.CduProg, 1, 3);
            Fn("MENU", Vns430Command.CduMenu, 1, 4);
            Fn("NAV\nRAD", Vns430Command.CduNavRad, 1, 5);
            Fn("BRT-", Vns430Command.CduBrightnessDown, 2, 0);
            Fn("PREV\nPAGE", Vns430Command.CduPrevPage, 2, 1);
            Fn("NEXT\nPAGE", Vns430Command.CduNextPage, 2, 2);
            Fn("EXEC", Vns430Command.CduExec, 2, 4);
            Fn("BRT+", Vns430Command.CduBrightnessUp, 2, 5);

            // Alpha block A..Y in a 5-wide grid (rows 4..8); Z on row 9.
            const string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXY";
            for (int i = 0; i < alpha.Length; i++)
            {
                keys.Add(new CduKey
                {
                    Legend = alpha[i].ToString(),
                    Command = (Vns430Command)((int)Vns430Command.CduAlphaA + i),
                    Kind = CduKeyKind.Alpha,
                    Row = 4 + (i / 5),
                    Col = i % 5
                });
            }
            keys.Add(new CduKey { Legend = "Z", Command = Vns430Command.CduAlphaZ, Kind = CduKeyKind.Alpha, Row = 9, Col = 0 });

            // Scratchpad-control keys on the alpha side.
            keys.Add(new CduKey { Legend = "SP", Command = Vns430Command.CduSpace, Kind = CduKeyKind.Symbol, Row = 9, Col = 1 });
            keys.Add(new CduKey { Legend = "DEL", Command = Vns430Command.CduDelete, Kind = CduKeyKind.Symbol, Row = 9, Col = 2 });
            keys.Add(new CduKey { Legend = "/", Command = Vns430Command.CduSlash, Kind = CduKeyKind.Symbol, Row = 9, Col = 3 });
            keys.Add(new CduKey { Legend = "CLR", Command = Vns430Command.CduClear, Kind = CduKeyKind.Symbol, Row = 9, Col = 4 });

            // Numeric block (cols 6..8, rows 4..7).
            const string digits = "123456789";
            for (int i = 0; i < digits.Length; i++)
            {
                keys.Add(new CduKey
                {
                    Legend = digits[i].ToString(),
                    Command = (Vns430Command)((int)Vns430Command.CduDigit0 + i + 1),
                    Kind = CduKeyKind.Digit,
                    Row = 4 + (i / 3),
                    Col = 6 + (i % 3)
                });
            }
            keys.Add(new CduKey { Legend = ".", Command = Vns430Command.CduDot, Kind = CduKeyKind.Symbol, Row = 7, Col = 6 });
            keys.Add(new CduKey { Legend = "0", Command = Vns430Command.CduDigit0, Kind = CduKeyKind.Digit, Row = 7, Col = 7 });
            keys.Add(new CduKey { Legend = "+/-", Command = Vns430Command.CduPlusMinus, Kind = CduKeyKind.Symbol, Row = 7, Col = 8 });

            return keys;
        }

        public static bool IsCduKeypadCommand(Vns430Command command)
        {
            int c = (int)command;
            return c >= (int)Vns430Command.CduAlphaA && c <= (int)Vns430Command.CduBrightnessDown;
        }

        // The scratchpad character a key produces, or '\0' for a non-character key.
        public static char CharFor(Vns430Command command)
        {
            int c = (int)command;
            if (c >= (int)Vns430Command.CduAlphaA && c <= (int)Vns430Command.CduAlphaZ)
            {
                return (char)('A' + (c - (int)Vns430Command.CduAlphaA));
            }
            if (c >= (int)Vns430Command.CduDigit0 && c <= (int)Vns430Command.CduDigit9)
            {
                return (char)('0' + (c - (int)Vns430Command.CduDigit0));
            }
            return command switch
            {
                Vns430Command.CduSpace => ' ',
                Vns430Command.CduDot => '.',
                Vns430Command.CduSlash => '/',
                _ => '\0'
            };
        }
    }
}
