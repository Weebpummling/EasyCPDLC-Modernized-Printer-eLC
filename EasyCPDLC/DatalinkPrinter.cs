using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyCPDLC
{
    internal enum DatalinkPrinterMode
    {
        Windows,
        RawEscPos
    }

    internal enum DatalinkMessageCategory
    {
        Other,
        PdcDcl,
        Cpdlc,
        TelexAoc,
        Atis
    }

    internal sealed class DatalinkPrinterSettings
    {
        public DatalinkPrinterMode Mode { get; init; } = DatalinkPrinterMode.Windows;
        public string PrinterName { get; init; } = string.Empty;
        public int Columns { get; init; } = 48;
        public bool AutoPrintPdcDcl { get; init; }
        public bool AutoPrintCpdlc { get; init; }
        public bool AutoPrintTelexAoc { get; init; }
        public bool AutoPrintAtis { get; init; }
        public bool AutoCut { get; init; }
        public int FeedLines { get; init; } = 3;
    }

    internal sealed class DatalinkPrintJob
    {
        public string Sender { get; init; } = "UNKNOWN";
        public string MessageType { get; init; } = "TELEX";
        public string Message { get; init; } = string.Empty;
        public DateTime ReceivedLocalTime { get; init; } = DateTime.Now;
        public DatalinkMessageCategory Category { get; init; } = DatalinkMessageCategory.Other;

        public static DatalinkPrintJob FromMessage(CPDLCMessage message)
        {
            return new DatalinkPrintJob
            {
                Sender = string.IsNullOrWhiteSpace(message?.recipient) ? "UNKNOWN" : message.recipient.Trim(),
                MessageType = string.IsNullOrWhiteSpace(message?.type) ? "TELEX" : message.type.Trim(),
                Message = message?.message?.Trim() ?? string.Empty,
                ReceivedLocalTime = DateTime.Now,
                Category = DatalinkPrinter.ClassifyMessage(message)
            };
        }
    }

    internal sealed class DatalinkPrinterStatus
    {
        public bool IsReady { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    internal sealed class DatalinkPrintResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    internal static class DatalinkPrinter
    {
        private const uint PrinterStatusPaused = 0x00000001;
        private const uint PrinterStatusError = 0x00000002;
        private const uint PrinterStatusPaperOut = 0x00000010;
        private const uint PrinterStatusOffline = 0x00000080;
        private const uint PrinterStatusNotAvailable = 0x00001000;
        private const uint PrinterStatusUserIntervention = 0x00100000;
        private const uint PrinterStatusDoorOpen = 0x00400000;

        private static readonly Regex LoadsheetWeightPattern = new(
            @"\b(?:ZFW|TOW|LAW|LDW|MZFW|MTOW|MLDW|MACZFW|MACTOW|MACLAW|MACLDW)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ClearancePattern = new(
            @"\b(?:PDC|DCL|PREDEP|PRE-DEPARTURE|CLEARANCE|CLRD|CLD)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DocInfo1
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string DocumentName;
            [MarshalAs(UnmanagedType.LPWStr)] public string OutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string DataType;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PrinterInfo2
        {
            public IntPtr ServerName;
            public IntPtr PrinterName;
            public IntPtr ShareName;
            public IntPtr PortName;
            public IntPtr DriverName;
            public IntPtr Comment;
            public IntPtr Location;
            public IntPtr DevMode;
            public IntPtr SeparatorFile;
            public IntPtr PrintProcessor;
            public IntPtr DataType;
            public IntPtr Parameters;
            public IntPtr SecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint JobCount;
            public uint AveragePagesPerMinute;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaults);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", EntryPoint = "GetPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetPrinter(IntPtr printerHandle, uint level, IntPtr buffer, uint bufferSize, out uint needed);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint StartDocPrinter(IntPtr printerHandle, uint level, [In] ref DocInfo1 documentInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr printerHandle, IntPtr buffer, uint byteCount, out uint bytesWritten);

        public static IReadOnlyList<string> GetInstalledPrinterNames()
        {
            try
            {
                return PrinterSettings.InstalledPrinters.Cast<string>()
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static string GetDefaultPrinterName()
        {
            try
            {
                PrinterSettings settings = new();
                return settings.IsValid ? settings.PrinterName ?? string.Empty : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool IsPrintableMessage(CPDLCMessage message)
        {
            return message != null &&
                !message.outbound &&
                !string.IsNullOrWhiteSpace(message.message) &&
                ClassifyMessage(message) != DatalinkMessageCategory.Other;
        }

        public static DatalinkMessageCategory ClassifyMessage(CPDLCMessage message)
        {
            if (message == null || message.outbound || string.IsNullOrWhiteSpace(message.message))
            {
                return DatalinkMessageCategory.Other;
            }

            string type = (message.type ?? string.Empty).Trim().ToUpperInvariant();
            string text = message.message.Trim();
            string combined = type + " " + text;

            if (ClearancePattern.IsMatch(combined))
            {
                return DatalinkMessageCategory.PdcDcl;
            }

            if (string.Equals(type, "ATIS", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains(" ATIS ", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("_ATIS", StringComparison.OrdinalIgnoreCase))
            {
                return DatalinkMessageCategory.Atis;
            }

            if (string.Equals(type, "CPDLC", StringComparison.OrdinalIgnoreCase))
            {
                return DatalinkMessageCategory.Cpdlc;
            }

            if (string.Equals(type, "TELEX", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "INFO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "AOC", StringComparison.OrdinalIgnoreCase))
            {
                return DatalinkMessageCategory.TelexAoc;
            }

            return DatalinkMessageCategory.Other;
        }

        public static bool IsELoadControlLoadsheet(CPDLCMessage message)
        {
            if (!IsPrintableMessage(message))
            {
                return false;
            }

            string text = message.message;
            if (text.Contains("ELOADCONTROL", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("LOADSHEET", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("LOAD PLANNING", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("SANDBOX ENVIRONMENT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return LoadsheetWeightPattern.Matches(text).Count >= 2;
        }

        public static bool ShouldAutoPrint(DatalinkPrintJob job, DatalinkPrinterSettings settings)
        {
            if (job == null || settings == null)
            {
                return false;
            }

            return job.Category switch
            {
                DatalinkMessageCategory.PdcDcl => settings.AutoPrintPdcDcl,
                DatalinkMessageCategory.Cpdlc => settings.AutoPrintCpdlc,
                DatalinkMessageCategory.TelexAoc => settings.AutoPrintTelexAoc,
                DatalinkMessageCategory.Atis => settings.AutoPrintAtis,
                _ => false
            };
        }

        public static DatalinkPrintJob CreateTestJob()
        {
            return new DatalinkPrintJob
            {
                Sender = "ELOADCONTROL",
                MessageType = "TELEX",
                Category = DatalinkMessageCategory.TelexAoc,
                ReceivedLocalTime = new DateTime(2026, 7, 20, 18, 42, 0, DateTimeKind.Utc).ToLocalTime(),
                Message =
                    "FLT UAL123       A/C B737-800\n" +
                    "FROM KSFO        TO   KLAX\n" +
                    "FINAL LOADSHEET\n" +
                    "PAX  154\n" +
                    "ZFW  137.4\n" +
                    "TOW  151.8\n" +
                    "TRIM 5.3 UNITS"
            };
        }

        public static DatalinkPrinterStatus GetPrinterStatus(string configuredPrinterName)
        {
            string printerName = ResolvePrinterName(configuredPrinterName);
            if (string.IsNullOrWhiteSpace(printerName))
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "NO PRINTER" };
            }

            if (!OpenPrinter(printerName, out IntPtr handle, IntPtr.Zero) || handle == IntPtr.Zero)
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "UNAVAILABLE" };
            }

            IntPtr buffer = IntPtr.Zero;
            try
            {
                _ = GetPrinter(handle, 2, IntPtr.Zero, 0, out uint needed);
                if (needed == 0)
                {
                    return new DatalinkPrinterStatus { IsReady = true, Message = "READY" };
                }

                buffer = Marshal.AllocHGlobal((int)needed);
                if (!GetPrinter(handle, 2, buffer, needed, out _))
                {
                    return new DatalinkPrinterStatus { IsReady = false, Message = "STATUS ERROR" };
                }

                PrinterInfo2 info = Marshal.PtrToStructure<PrinterInfo2>(buffer);
                return StatusFromFlags(info.Status, info.JobCount);
            }
            catch
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "STATUS ERROR" };
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
                ClosePrinter(handle);
            }
        }

        public static DatalinkPrintResult Print(DatalinkPrintJob job, DatalinkPrinterSettings settings)
        {
            if (job == null || string.IsNullOrWhiteSpace(job.Message))
            {
                return Failed("NO PRINTABLE ACARS MESSAGE IS AVAILABLE.");
            }

            settings ??= new DatalinkPrinterSettings();
            string printerName = ResolvePrinterName(settings.PrinterName);
            if (string.IsNullOrWhiteSpace(printerName))
            {
                return Failed("NO WINDOWS PRINTER IS INSTALLED OR SELECTED.");
            }

            DatalinkPrinterStatus status = GetPrinterStatus(printerName);
            if (!status.IsReady)
            {
                return Failed("PRINTER " + status.Message + ": " + printerName);
            }

            return settings.Mode == DatalinkPrinterMode.RawEscPos
                ? PrintRawEscPos(printerName, job, settings)
                : PrintWindows(printerName, job, settings);
        }

        public static string FormatReceiptText(DatalinkPrintJob job, int requestedColumns)
        {
            int columns = NormalizeColumns(requestedColumns);
            List<string> lines = new()
            {
                Center("ACARS", columns),
                new string('-', columns)
            };

            string metadata = "TYPE " + SafeToken(job?.MessageType, "TELEX") + "  FROM " + SafeToken(job?.Sender, "UNKNOWN");
            lines.AddRange(WrapLines(metadata, columns));
            lines.Add(new string('-', columns));
            lines.AddRange(WrapLines(job?.Message ?? string.Empty, columns));
            lines.Add(new string('-', columns));
            lines.Add(Center((job?.ReceivedLocalTime ?? DateTime.Now).ToUniversalTime().ToString("ddMMMyy HHmm'Z'").ToUpperInvariant(), columns));
            return string.Join("\n", lines);
        }

        private static DatalinkPrintResult PrintWindows(string printerName, DatalinkPrintJob job, DatalinkPrinterSettings settings)
        {
            List<string> lines = FormatReceiptText(job, settings.Columns).Split('\n').ToList();
            int nextLine = 0;

            try
            {
                using PrintDocument document = new();
                document.DocumentName = "EasyCPDLC ACARS " + job.MessageType;
                document.PrintController = new StandardPrintController();
                document.PrinterSettings.PrinterName = printerName;
                document.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);

                if (!document.PrinterSettings.IsValid)
                {
                    return Failed("THE SELECTED WINDOWS PRINTER IS INVALID: " + printerName);
                }

                document.PrintPage += (_, e) =>
                {
                    using Font bodyFont = new("Consolas", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
                    using Font headingFont = new("Consolas", 13.5f, FontStyle.Bold, GraphicsUnit.Point);
                    float lineHeight = bodyFont.GetHeight(e.Graphics) + 1f;
                    int linesPerPage = Math.Max(1, (int)Math.Floor(e.MarginBounds.Height / lineHeight));
                    int pageLine = 0;

                    while (nextLine < lines.Count && pageLine < linesPerPage)
                    {
                        string line = lines[nextLine];
                        Font font = nextLine == 0 ? headingFont : bodyFont;
                        RectangleF lineBounds = new(
                            e.MarginBounds.Left,
                            e.MarginBounds.Top + (pageLine * lineHeight),
                            e.MarginBounds.Width,
                            Math.Max(lineHeight, font.GetHeight(e.Graphics) + 1f));

                        using StringFormat format = new()
                        {
                            Alignment = nextLine == 0 ? StringAlignment.Center : StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            Trimming = StringTrimming.None
                        };
                        e.Graphics.DrawString(line.TrimEnd(), font, Brushes.Black, lineBounds, format);
                        nextLine++;
                        pageLine++;
                    }

                    e.HasMorePages = nextLine < lines.Count;
                };

                document.Print();
                return Succeeded("PRINT JOB SENT TO " + printerName);
            }
            catch (Exception ex)
            {
                return Failed("WINDOWS PRINT FAILED: " + ex.Message);
            }
        }

        private static DatalinkPrintResult PrintRawEscPos(string printerName, DatalinkPrintJob job, DatalinkPrinterSettings settings)
        {
            byte[] payload = BuildEscPosPayload(job, settings);
            IntPtr handle = IntPtr.Zero;
            IntPtr unmanaged = IntPtr.Zero;
            bool documentStarted = false;
            bool pageStarted = false;

            try
            {
                if (!OpenPrinter(printerName, out handle, IntPtr.Zero) || handle == IntPtr.Zero)
                {
                    return Failed("RAW PRINTER OPEN FAILED: " + Marshal.GetLastWin32Error());
                }

                DocInfo1 documentInfo = new()
                {
                    DocumentName = "EasyCPDLC ACARS " + job.MessageType,
                    OutputFile = null,
                    DataType = "RAW"
                };

                if (StartDocPrinter(handle, 1, ref documentInfo) == 0)
                {
                    return Failed("RAW PRINT DOCUMENT FAILED: " + Marshal.GetLastWin32Error());
                }
                documentStarted = true;

                if (!StartPagePrinter(handle))
                {
                    return Failed("RAW PRINT PAGE FAILED: " + Marshal.GetLastWin32Error());
                }
                pageStarted = true;

                unmanaged = Marshal.AllocCoTaskMem(payload.Length);
                Marshal.Copy(payload, 0, unmanaged, payload.Length);
                if (!WritePrinter(handle, unmanaged, (uint)payload.Length, out uint written) || written != payload.Length)
                {
                    return Failed("RAW PRINTER WRITE FAILED: " + Marshal.GetLastWin32Error());
                }

                return Succeeded("ESC/POS JOB SENT TO " + printerName);
            }
            catch (Exception ex)
            {
                return Failed("ESC/POS PRINT FAILED: " + ex.Message);
            }
            finally
            {
                if (unmanaged != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(unmanaged);
                }
                if (pageStarted)
                {
                    EndPagePrinter(handle);
                }
                if (documentStarted)
                {
                    EndDocPrinter(handle);
                }
                if (handle != IntPtr.Zero)
                {
                    ClosePrinter(handle);
                }
            }
        }

        private static byte[] BuildEscPosPayload(DatalinkPrintJob job, DatalinkPrinterSettings settings)
        {
            int columns = NormalizeColumns(settings.Columns);
            List<byte> bytes = new();

            Add(bytes, 0x1B, 0x40);             // Initialize.
            Add(bytes, 0x1B, 0x61, 0x01);       // Center.
            Add(bytes, 0x1B, 0x45, 0x01);       // Bold on.
            Add(bytes, 0x1D, 0x21, 0x11);       // Double width and height.
            AddAscii(bytes, "ACARS\n");
            Add(bytes, 0x1D, 0x21, 0x00);       // Normal size.
            Add(bytes, 0x1B, 0x45, 0x00);       // Bold off.
            Add(bytes, 0x1B, 0x61, 0x00);       // Left.

            string receipt = FormatReceiptText(job, columns);
            string body = string.Join("\n", receipt.Split('\n').Skip(1));
            AddAscii(bytes, body + "\n");

            int feeds = Math.Max(0, Math.Min(12, settings.FeedLines));
            if (feeds > 0)
            {
                Add(bytes, 0x1B, 0x64, (byte)feeds); // Feed n lines.
            }

            if (settings.AutoCut)
            {
                Add(bytes, 0x1D, 0x56, 0x00); // Full cut.
            }

            return bytes.ToArray();
        }

        private static DatalinkPrinterStatus StatusFromFlags(uint status, uint jobCount)
        {
            if ((status & PrinterStatusPaperOut) != 0)
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "PAPER OUT" };
            }
            if ((status & PrinterStatusDoorOpen) != 0)
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "DOOR OPEN" };
            }
            if ((status & PrinterStatusOffline) != 0)
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "OFFLINE" };
            }
            if ((status & PrinterStatusPaused) != 0)
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "PAUSED" };
            }
            if ((status & (PrinterStatusError | PrinterStatusNotAvailable | PrinterStatusUserIntervention)) != 0)
            {
                return new DatalinkPrinterStatus { IsReady = false, Message = "ATTENTION" };
            }

            return new DatalinkPrinterStatus
            {
                IsReady = true,
                Message = jobCount > 0 ? "READY / " + jobCount + " QUEUED" : "READY"
            };
        }

        private static string ResolvePrinterName(string configuredPrinterName)
        {
            string configured = (configuredPrinterName ?? string.Empty).Trim();
            IReadOnlyList<string> installed = GetInstalledPrinterNames();

            string match = installed.FirstOrDefault(name => string.Equals(name, configured, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
            {
                return match;
            }

            string defaultPrinter = GetDefaultPrinterName();
            return installed.FirstOrDefault(name => string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase))
                ?? installed.FirstOrDefault()
                ?? string.Empty;
        }

        private static int NormalizeColumns(int requestedColumns)
        {
            return requestedColumns >= 56 ? 64 : 48;
        }

        private static string SafeToken(string value, string fallback)
        {
            string cleaned = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
        }

        private static string Center(string value, int width)
        {
            string cleaned = (value ?? string.Empty).Trim();
            if (cleaned.Length >= width)
            {
                return cleaned.Substring(0, width);
            }

            int left = (width - cleaned.Length) / 2;
            return new string(' ', left) + cleaned;
        }

        private static IEnumerable<string> WrapLines(string text, int width)
        {
            List<string> output = new();
            foreach (string rawLine in (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string remaining = rawLine.TrimEnd();
                if (remaining.Length == 0)
                {
                    output.Add(string.Empty);
                    continue;
                }

                while (remaining.Length > width)
                {
                    int splitAt = remaining.LastIndexOf(' ', width);
                    if (splitAt < 1)
                    {
                        splitAt = width;
                    }

                    output.Add(remaining.Substring(0, splitAt).TrimEnd());
                    remaining = remaining.Substring(splitAt).TrimStart();
                }
                output.Add(remaining);
            }
            return output;
        }

        private static void Add(List<byte> target, params byte[] values)
        {
            target.AddRange(values);
        }

        private static void AddAscii(List<byte> target, string value)
        {
            target.AddRange(Encoding.ASCII.GetBytes(value ?? string.Empty));
        }

        private static DatalinkPrintResult Succeeded(string message)
        {
            return new DatalinkPrintResult { Success = true, Message = message };
        }

        private static DatalinkPrintResult Failed(string message)
        {
            return new DatalinkPrintResult { Success = false, Message = message };
        }
    }
}
