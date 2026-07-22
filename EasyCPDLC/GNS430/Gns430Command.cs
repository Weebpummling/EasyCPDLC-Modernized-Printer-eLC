namespace EasyCPDLC.GNS430
{
    internal enum Gns430Command : byte
    {
        None = 0,
        LargeRightDecrease = 1,
        LargeRightIncrease = 2,
        SmallRightDecrease = 3,
        SmallRightIncrease = 4,
        CursorPush = 5,
        Enter = 6,
        Clear = 7,
        Menu = 8,
        Message = 9,
        FlightPlan = 10,
        Procedure = 11,
        DirectTo = 12,
        Obs = 13,
        Cdi = 14,
        RangeIn = 15,
        RangeOut = 16,
        Nearest = 17,
        Power = 18
    }
}
