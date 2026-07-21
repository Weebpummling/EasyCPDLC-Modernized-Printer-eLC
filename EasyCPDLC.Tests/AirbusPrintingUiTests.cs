using EasyCPDLC;
using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace EasyCPDLC.Tests
{
    public sealed class AirbusPrintingUiTests
    {
        [Fact]
        public void AirbusMainAsset_HasExpectedFiveLskPanelGeometry()
        {
            using Image asset = EmbeddedAssets.LoadImage("Resources", "DCDU_Main_V15.png");

            Assert.NotNull(asset);
            Assert.Equal(700, asset.Width);
            Assert.Equal(350, asset.Height);
        }

        [Fact]
        public void AirbusMessagePreview_MapsPrintAndReprintToLeftLskFourAndFive()
        {
            Assert.True(MainForm.IsStyledPreviewPrintLsk(false, false, 4));
            Assert.True(MainForm.IsStyledPreviewReprintLsk(false, false, 5));

            Assert.False(MainForm.IsStyledPreviewPrintLsk(false, true, 4));
            Assert.False(MainForm.IsStyledPreviewPrintLsk(false, false, 5));
            Assert.False(MainForm.IsStyledPreviewReprintLsk(false, true, 5));
            Assert.False(MainForm.IsStyledPreviewReprintLsk(false, false, 4));
        }

        [Fact]
        public void BoeingMessagePreview_RetainsItsOwnSixKeyMapping()
        {
            Assert.True(MainForm.IsStyledPreviewPrintLsk(true, false, 5));
            Assert.True(MainForm.IsStyledPreviewReprintLsk(true, false, 6));
            Assert.False(MainForm.IsStyledPreviewPrintLsk(true, false, 4));
            Assert.False(MainForm.IsStyledPreviewReprintLsk(true, false, 5));
        }

        [Fact]
        public void BoeingConnectHotspot_CoversTheCompletePaintedKey()
        {
            using Image asset = EmbeddedAssets.LoadImage("Resources", "DCDU_Main_V15_Boeing.png");
            Rectangle bounds = MainForm.BoeingConnectButtonBounds();

            Assert.NotNull(asset);
            Assert.True(bounds.Left >= 0 && bounds.Top >= 0);
            Assert.True(bounds.Right <= asset.Width && bounds.Bottom <= asset.Height);
            Assert.True(bounds.Contains(new Point(45, 357)));
            Assert.True(bounds.Contains(new Point(68, 369)));
        }

        [Theory]
        [InlineData(false, false, "")]
        [InlineData(true, true, "")]
        [InlineData(false, true, "VATSIM CONNECTED.")]
        [InlineData(true, false, "VATSIM DISCONNECTED.")]
        public void VatsimStatusMessages_AreOnlyProducedForConnectionTransitions(
            bool wasConnected,
            bool isConnected,
            string expected)
        {
            Assert.Equal(expected, MainForm.VatsimConnectionTransitionMessage(wasConnected, isConnected));
        }

        [Fact]
        public void PrinterDropdown_IsExemptFromTopLevelFocusRecapture()
        {
            using ComboBox printerSelector = new();
            using Button ordinaryButton = new();

            Assert.False(MainForm.ShouldCaptureFocusForControl(printerSelector));
            Assert.True(MainForm.ShouldCaptureFocusForControl(ordinaryButton));
        }
    }
}
