using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EasyCPDLC.VPilotBridge.Protocol
{
    public sealed class VpilotContactDetails
    {
        public string ControllerCallsign { get; set; } = string.Empty;
        public string Facility { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string OriginalMessage { get; set; } = string.Empty;
    }

    public static class VpilotContactMeParser
    {
        private static readonly Regex ControllerCallsign = new Regex(
            @"_(?<type>DEL|GND|TWR|DEP|APP|CTR|FSS|RDO|RAD|CTL|CON|CONTROL)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ContactMarker = new Regex(
            @"\b(?:PLEASE\s+)?CONTACT(?:\s+(?:ME|[A-Z0-9_]{2,}))?\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DecimalFrequency = new Regex(
            @"(?<!\d)(?<whole>1(?:1[89]|2\d|3[0-6]))[\.,](?<fraction>\d{1,3})(?!\d)",
            RegexOptions.Compiled);

        private static readonly Regex CompactFrequency = new Regex(
            @"(?<!\d)(?<frequency>1(?:1[89]|2\d|3[0-6])\d{3})(?!\d)",
            RegexOptions.Compiled);

        public static bool TryParse(
            string controllerCallsign,
            string message,
            int fallbackControllerFrequency,
            out VpilotContactDetails details)
        {
            details = null;
            string callsign = (controllerCallsign ?? string.Empty).Trim().ToUpperInvariant();
            string body = NormalizeLineEndings(message).Trim();

            if (!IsControllerCallsign(callsign) ||
                body.Length == 0 ||
                !ContactMarker.IsMatch(body))
            {
                return false;
            }

            string frequency = TryExtractFrequency(body);
            if (frequency.Length == 0)
            {
                frequency = FormatControllerFrequency(fallbackControllerFrequency);
            }

            if (frequency.Length == 0)
            {
                return false;
            }

            details = new VpilotContactDetails
            {
                ControllerCallsign = callsign,
                Facility = GetFacilityLabel(callsign),
                Frequency = frequency,
                OriginalMessage = body
            };
            return true;
        }

        public static string FormatDisplayMessage(VpilotContactDetails details)
        {
            if (details == null)
            {
                throw new ArgumentNullException(nameof(details));
            }

            string callsign = (details.ControllerCallsign ?? string.Empty).Trim().ToUpperInvariant();
            string facility = string.IsNullOrWhiteSpace(details.Facility)
                ? GetFacilityLabel(callsign)
                : details.Facility.Trim().ToUpperInvariant();
            string frequency = (details.Frequency ?? string.Empty).Trim();
            string original = NormalizeLineEndings(details.OriginalMessage).Trim().ToUpperInvariant();

            string result = "ATC CONTACT REQUEST\n" +
                "FACILITY: " + facility + "\n" +
                "CALLSIGN: " + callsign + "\n" +
                "FREQUENCY: " + frequency;

            if (original.Length > 0)
            {
                result += "\nMESSAGE: " + original;
            }

            return result;
        }

        public static bool IsControllerCallsign(string callsign)
        {
            return ControllerCallsign.IsMatch((callsign ?? string.Empty).Trim());
        }

        public static string GetFacilityLabel(string callsign)
        {
            string normalized = (callsign ?? string.Empty).Trim().ToUpperInvariant();
            Match match = ControllerCallsign.Match(normalized);
            if (!match.Success)
            {
                return normalized.Length == 0 ? "ATC" : normalized + " CONTROL";
            }

            string prefix = normalized.Substring(0, match.Index).Replace('_', ' ').Trim();
            string type = match.Groups["type"].Value.ToUpperInvariant();
            string service;
            switch (type)
            {
                case "DEL": service = "DELIVERY"; break;
                case "GND": service = "GROUND"; break;
                case "TWR": service = "TOWER"; break;
                case "DEP": service = "DEPARTURE"; break;
                case "APP": service = "APPROACH"; break;
                case "CTR": service = "CENTER"; break;
                case "FSS":
                case "RDO":
                case "RAD": service = "RADIO"; break;
                default: service = "CONTROL"; break;
            }

            return (prefix + " " + service).Trim();
        }

        public static string FormatControllerFrequency(int frequency)
        {
            double mhz;
            if (frequency >= 118000000 && frequency <= 136999999)
            {
                mhz = frequency / 1000000d;
            }
            else if (frequency >= 118000 && frequency <= 136999)
            {
                mhz = frequency / 1000d;
            }
            else if (frequency >= 18000 && frequency <= 36999)
            {
                // vPilot's radio-event convention drops the leading 1 and decimal point.
                mhz = (frequency + 100000) / 1000d;
            }
            else
            {
                return string.Empty;
            }

            return mhz >= 118d && mhz < 137d
                ? mhz.ToString("000.000", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string TryExtractFrequency(string message)
        {
            Match decimalMatch = DecimalFrequency.Match(message ?? string.Empty);
            if (decimalMatch.Success)
            {
                string fraction = decimalMatch.Groups["fraction"].Value.PadRight(3, '0');
                return decimalMatch.Groups["whole"].Value + "." + fraction;
            }

            Match compactMatch = CompactFrequency.Match(message ?? string.Empty);
            if (compactMatch.Success &&
                int.TryParse(compactMatch.Groups["frequency"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int compact))
            {
                return FormatControllerFrequency(compact);
            }

            return string.Empty;
        }

        private static string NormalizeLineEndings(string value)
        {
            return (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}
