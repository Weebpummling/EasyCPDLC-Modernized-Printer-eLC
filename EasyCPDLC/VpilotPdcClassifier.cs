using System;
using System.Text.RegularExpressions;

namespace EasyCPDLC
{
    internal static class VpilotPdcClassifier
    {
        private static readonly Regex PdcMarker = new(
            @"\b(?:PDC|PRE[ -]?DEPARTURE CLEARANCE|PREDEP CLEARANCE)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ClearanceMarker = new(
            @"\b(?:CLEARED|CLRD|CLEARANCE|SQUAWK|BEACON CODE|DEPARTURE FREQUENCY|DPFRQ|SID)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsPdc(string sender, string message)
        {
            string source = (sender ?? string.Empty).Trim();
            string body = (message ?? string.Empty).Trim();
            if (body.Length == 0)
            {
                return false;
            }

            bool fromAcars = string.Equals(source, "ACARS", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("_ACARS", StringComparison.OrdinalIgnoreCase);
            bool hasPdcMarker = PdcMarker.IsMatch(body);
            bool hasClearanceMarker = ClearanceMarker.IsMatch(body);

            // vTDLS normally delivers from the ACARS pseudo-station. Requiring at
            // least one clearance marker avoids consuming unrelated VATSIM PMs.
            return (fromAcars && (hasPdcMarker || hasClearanceMarker)) ||
                (hasPdcMarker && hasClearanceMarker);
        }
    }
}
