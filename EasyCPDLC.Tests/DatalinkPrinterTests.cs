using System;
using System.IO;
using System.Linq;
using System.Globalization;
using EasyCPDLC;
using Xunit;

namespace EasyCPDLC.Tests
{
    public sealed class DatalinkPrinterTests
    {
        [Fact]
        public void FormatReceiptText_IncludesMetadataAndRespects48Columns()
        {
            DatalinkPrintJob job = DatalinkPrinter.CreateTestJob();
            string receipt = DatalinkPrinter.FormatReceiptText(job, 48);
            string[] lines = receipt.Split('\n');

            Assert.Contains("TYPE LOADSHEET", receipt);
            Assert.Contains("UTC 20JUL26 1842Z", receipt);
            Assert.Contains("A/C B-18662", receipt);
            Assert.Contains("FROM DISPATCH", receipt);
            Assert.Contains("END OF LOADSHEET", receipt);
            Assert.All(lines, line => Assert.True(line.Length <= 48, "Over-width line: " + line));
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("ja-JP")]
        [InlineData("tr-TR")]
        public void FormatReceiptText_UsesInvariantUtcTimestampAcrossUserCultures(string cultureName)
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                string receipt = DatalinkPrinter.FormatReceiptText(DatalinkPrinter.CreateTestJob(), 69);

                Assert.Contains("UTC 20JUL26 1842Z", receipt);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Fact]
        public void WrapLines_PreservesBreaksAndAlignmentAndWrapsAtWords()
        {
            string[] wrapped = DatalinkPrinter.WrapLines(
                "FLT CI7752    SFO-SNA\r\n    ALIGNED VALUE\rTHIS IS A LONG LINE THAT WRAPS AT A WORD",
                24).ToArray();

            Assert.Equal("FLT CI7752    SFO-SNA", wrapped[0]);
            Assert.Equal("    ALIGNED VALUE", wrapped[1]);
            Assert.Equal("THIS IS A LONG LINE", wrapped[2]);
            Assert.Equal("THAT WRAPS AT A WORD", wrapped[3]);
        }

        [Fact]
        public void Cp437Encoding_UsesWesternCharacterAndSafeReplacement()
        {
            byte[] encoded = DatalinkPrinter.EncodePrinterText("Café €");
            Assert.Equal(0x82, encoded[3]);
            Assert.Equal((byte)'?', encoded[^1]);
        }

        [Fact]
        public void VtdlsPdc_UsesBridgeCallsignAndIdentifiesTransportOnReceipt()
        {
            CPDLCMessage message = new("PDC", "KSFO_DEL", "PDC CLEARED TO KSNA", false)
            {
                aircraftCallsign = "CI7752",
                transport = "VATSIM/VTDLS"
            };

            DatalinkPrintJob job = DatalinkPrintJob.FromMessage(message, "WRONG1");
            string receipt = DatalinkPrinter.FormatReceiptText(job, 48);

            Assert.Equal(DatalinkMessageCategory.PdcDcl, job.Category);
            Assert.Contains("A/C CI7752", receipt);
            Assert.Contains("SOURCE VATSIM/VTDLS", receipt);
        }

        [Fact]
        public void DuplicateSuppression_UsesStableIdAndExpires()
        {
            DatalinkPrintDeduplicator deduplicator = new(TimeSpan.FromMinutes(10));
            DateTime start = new(2026, 7, 20, 18, 42, 0, DateTimeKind.Utc);

            Assert.True(deduplicator.TryRegister("MESSAGE-42", start));
            Assert.False(deduplicator.TryRegister("MESSAGE-42", start.AddMinutes(1)));
            Assert.True(deduplicator.TryRegister("MESSAGE-43", start.AddMinutes(1)));
            Assert.True(deduplicator.TryRegister("MESSAGE-42", start.AddMinutes(11)));
        }

        [Fact]
        public void StableId_PrefersHoppieMessageIdOtherwiseHashesCanonicalContent()
        {
            CPDLCMessage withHeader = new("CPDLC", "EDYY", "CLIMB FL350", false,
                new CPDLCResponse { MessageID = 29 });
            string hoppieId = DatalinkPrinter.DeriveStableMessageId(withHeader, "CI7752");
            string hash1 = DatalinkPrinter.DeriveStableMessageId("TELEX", "DISPATCH", "CI7752", "A\r\nB");
            string hash2 = DatalinkPrinter.DeriveStableMessageId("TELEX", "DISPATCH", "CI7752", "A\nB");

            Assert.Equal("HOPPIE-CPDLC-EDYY-29", hoppieId);
            Assert.Equal(hash1, hash2);
            Assert.StartsWith("SHA256-", hash1);
        }

        [Fact]
        public void EscPos48Payload_BeginsWithInitializeAndEndsWithFeedAndPartialCut()
        {
            byte[] payload = DatalinkPrinter.BuildEscPosPayload(
                DatalinkPrinter.CreateTestJob(),
                new DatalinkPrinterSettings
                {
                    Profile = DatalinkPrinterProfile.GenericEscPos80Mm,
                    Columns = 48,
                    FeedLines = 3,
                    CutMode = DatalinkCutMode.Partial
                });

            Assert.True(payload.Take(2).SequenceEqual(new byte[] { 0x1B, 0x40 }));
            Assert.True(Contains(payload, 0x1B, 0x61, 0x00));
            Assert.True(Contains(payload, 0x1B, 0x74, 0x00));
            Assert.True(Contains(payload, 0x1B, 0x4D, 0x00));
            Assert.True(payload.TakeLast(7).SequenceEqual(new byte[] { 0x1B, 0x64, 0x03, 0x1D, 0x56, 0x42, 0x00 }));
        }

        [Fact]
        public void EscPos64Payload_SelectsFontB()
        {
            byte[] payload = DatalinkPrinter.BuildEscPosPayload(
                DatalinkPrinter.CreateTestJob(),
                new DatalinkPrinterSettings
                {
                    Profile = DatalinkPrinterProfile.GenericEscPos80Mm,
                    Columns = 64,
                    FeedLines = 0,
                    CutMode = DatalinkCutMode.Off
                });

            Assert.True(Contains(payload, 0x1B, 0x4D, 0x01));
            Assert.False(payload.TakeLast(4).SequenceEqual(new byte[] { 0x1D, 0x56, 0x42, 0x00 }));
        }

        [Fact]
        public void Citizen112Profile_DefaultsTo69ColumnsAndCondensedUses92ColumnFontB()
        {
            DatalinkPrinterSettings defaults = new();
            DatalinkPrintJob job = DatalinkPrinter.CreateTestJob();
            byte[] normal = DatalinkPrinter.BuildEscPosPayload(job, defaults);
            byte[] condensed = DatalinkPrinter.BuildEscPosPayload(
                job,
                new DatalinkPrinterSettings
                {
                    Profile = DatalinkPrinterProfile.CitizenCtS4000_112Mm,
                    Columns = 92,
                    FeedLines = 0,
                    CutMode = DatalinkCutMode.Off
                });

            Assert.Equal(DatalinkPrinterProfile.CitizenCtS4000_112Mm, defaults.Profile);
            Assert.Equal(69, defaults.Columns);
            Assert.Equal(69, DatalinkPrinter.NormalizeProfileColumns(defaults.Profile, 48));
            Assert.Equal(92, DatalinkPrinter.NormalizeProfileColumns(defaults.Profile, 92));
            Assert.Equal(48, DatalinkPrinter.NormalizeProfileColumns(DatalinkPrinterProfile.GenericEscPos80Mm, defaults.Columns));
            Assert.True(Contains(normal, 0x1B, 0x4D, 0x00));
            Assert.True(Contains(condensed, 0x1B, 0x4D, 0x01));
            Assert.All(DatalinkPrinter.FormatReceiptText(job, 92).Split('\n'), line =>
                Assert.True(line.Length <= 92, "Over-width line: " + line));
        }

        [Fact]
        public void WidePrinterProfile_UsesGenericFourInchLabelAndMigratesLegacyNames()
        {
            Assert.Equal("GENERIC 4 INCH", DatalinkPrinter.GetProfileShortName(DatalinkPrinterProfile.CitizenCtS4000_112Mm));
            Assert.Equal(DatalinkPrinterProfile.CitizenCtS4000_112Mm, MainForm.ParsePrinterProfileSetting("GENERIC 4 INCH"));
            Assert.Equal(DatalinkPrinterProfile.CitizenCtS4000_112Mm, MainForm.ParsePrinterProfileSetting("CITIZEN CT-S4000 112MM"));
            Assert.Equal(DatalinkPrinterProfile.GenericEscPos80Mm, MainForm.ParsePrinterProfileSetting("GENERIC ESC/POS 80MM"));
        }

        [Fact]
        public void AirlineLayout_UsesFullWidthRulesAndRightAlignedMetadata()
        {
            string[] lines = DatalinkPrinter.FormatReceiptText(DatalinkPrinter.CreateTestJob(), 69).Split('\n');

            Assert.Equal(69, lines[1].Length);
            Assert.Equal(new string('-', 69), lines[1]);
            Assert.StartsWith("TYPE LOADSHEET", lines[2]);
            Assert.EndsWith("UTC 20JUL26 1842Z", lines[2]);
            Assert.Contains(lines, line => line.StartsWith("A/C B-18662") && line.EndsWith("FROM DISPATCH"));
            Assert.Equal(new string('-', 69), lines[^1]);
        }

        [Fact]
        public void FullCutMode_UsesConfigurableFullCutCommand()
        {
            byte[] payload = DatalinkPrinter.BuildEscPosPayload(
                DatalinkPrinter.CreateTestJob(),
                new DatalinkPrinterSettings
                {
                    Profile = DatalinkPrinterProfile.GenericEscPos80Mm,
                    Columns = 48,
                    FeedLines = 1,
                    CutMode = DatalinkCutMode.Full
                });

            Assert.True(payload.TakeLast(7).SequenceEqual(new byte[] { 0x1B, 0x64, 0x01, 0x1D, 0x56, 0x41, 0x00 }));
        }

        [Fact]
        public void SampleJobHexDump_IsCompleteAndCanBeRegenerated()
        {
            byte[] payload = DatalinkPrinter.BuildEscPosPayload(
                DatalinkPrinter.CreateTestJob(),
                new DatalinkPrinterSettings
                {
                    Profile = DatalinkPrinterProfile.GenericEscPos80Mm,
                    Columns = 48,
                    FeedLines = 3,
                    CutMode = DatalinkCutMode.Partial
                });
            string dump = DatalinkPrinter.ToHexDump(payload);

            Assert.StartsWith("0000  1B 40", dump);
            Assert.Contains("ACARS", dump);
            Assert.EndsWith(Environment.NewLine, dump);

            string outputPath = Environment.GetEnvironmentVariable("EASYCPDLC_SAMPLE_HEX_PATH");
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
                File.WriteAllText(outputPath, dump);
            }
        }

        [Fact]
        public void MockMode_WritesExactPayloadAndHexPreviewWithoutPrinterQueue()
        {
            string directory = Path.Combine(Path.GetTempPath(), "EasyCPDLC-printer-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                DatalinkPrintJob job = DatalinkPrinter.CreateTestJob();
                DatalinkPrinterSettings settings = new()
                {
                    Mode = DatalinkPrinterMode.MockFile,
                    Profile = DatalinkPrinterProfile.CitizenCtS4000_112Mm,
                    Columns = 69,
                    FeedLines = 3,
                    CutMode = DatalinkCutMode.Partial,
                    MockOutputDirectory = directory
                };

                DatalinkPrintResult result = DatalinkPrinter.Print(job, settings);

                Assert.True(result.Success, result.Message);
                Assert.True(File.Exists(result.OutputPath));
                Assert.Equal(DatalinkPrinter.BuildEscPosPayload(job, settings), File.ReadAllBytes(result.OutputPath));
                Assert.True(File.Exists(Path.ChangeExtension(result.OutputPath, ".hex.txt")));
                Assert.True(File.Exists(Path.ChangeExtension(result.OutputPath, ".preview.txt")));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact]
        public void Print_FailsClosed_WhenNoPrinterIsSelected_EscPos()
        {
            // A blank printer name means the user never chose a queue (the selector
            // starts on <SELECT PRINTER>). The job must not silently fall back to the
            // Windows default or first installed printer, which previously sent raw
            // ESC/POS to unrelated office printers.
            DatalinkPrintResult result = DatalinkPrinter.Print(
                DatalinkPrinter.CreateTestJob(),
                new DatalinkPrinterSettings { Mode = DatalinkPrinterMode.RawEscPos, PrinterName = string.Empty });

            Assert.False(result.Success);
            Assert.Contains("NO WINDOWS PRINTER", result.Message);
        }

        [Fact]
        public void Print_FailsClosed_WhenNoPrinterIsSelected_Windows()
        {
            DatalinkPrintResult result = DatalinkPrinter.Print(
                DatalinkPrinter.CreateTestJob(),
                new DatalinkPrinterSettings { Mode = DatalinkPrinterMode.Windows, PrinterName = string.Empty });

            Assert.False(result.Success);
            Assert.Contains("NO WINDOWS PRINTER", result.Message);
        }

        [Fact]
        public void Print_FailsClosed_WhenSelectedPrinterIsNotInstalled()
        {
            DatalinkPrintResult result = DatalinkPrinter.Print(
                DatalinkPrinter.CreateTestJob(),
                new DatalinkPrinterSettings
                {
                    Mode = DatalinkPrinterMode.RawEscPos,
                    PrinterName = "NO SUCH PRINTER " + Guid.NewGuid().ToString("N")
                });

            Assert.False(result.Success);
            Assert.Contains("NOT INSTALLED", result.Message);
        }

        private static bool Contains(byte[] source, params byte[] sequence)
        {
            return source.Select((_, index) => index).Any(index =>
                index + sequence.Length <= source.Length &&
                source.Skip(index).Take(sequence.Length).SequenceEqual(sequence));
        }
    }
}
