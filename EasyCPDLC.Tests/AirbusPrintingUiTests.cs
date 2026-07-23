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

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        public void SuccessfulFirstPrint_RefreshesOnlyAVisibleMessagePreview(
            bool reprintWasAvailable,
            bool hasVisiblePreview,
            bool expected)
        {
            Assert.Equal(
                expected,
                MainForm.ShouldRefreshStyledPreviewAfterPrint(reprintWasAvailable, hasVisiblePreview));
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

        [Theory]
        [InlineData("55", 60)]
        [InlineData("63", 65)]
        [InlineData("110", 110)]
        [InlineData("123%", 125)]
        [InlineData("250", 200)]
        [InlineData("invalid", 100)]
        public void WindowScale_AllowsFlexibleSafeFivePercentSteps(string value, int expected)
        {
            Assert.Equal(expected, MainForm.NormalizeWindowScalePercent(value));
        }

        [Fact]
        public void ScreenOnlyMode_UsesTheNativeDisplayAreaAsItsWindowBase()
        {
            Assert.Equal(new Size(493, 282), MainForm.MainWindowBaseSize(false, false));
            Assert.Equal(new Size(496, 282), MainForm.MainWindowBaseSize(true, false));
            Assert.Equal(new Size(700, 350), MainForm.MainWindowBaseSize(false, true));
            Assert.Equal(new Size(660, 450), MainForm.MainWindowBaseSize(true, true));
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void PanelHotspots_RemainAvailableInsideSetupForRecovery(
            bool enabled,
            bool setupActive,
            bool expected)
        {
            Assert.Equal(expected, MainForm.ArePanelHotspotsActive(enabled, setupActive));
        }

        [Fact]
        public void DragResize_IsConvertedBackToAProportionalSavedScale()
        {
            Assert.Equal(150, MainForm.CalculateWindowScalePercent(new Size(1050, 525), false, true));
            Assert.Equal(75, MainForm.CalculateWindowScalePercent(new Size(372, 212), true, false));
        }

        [Fact]
        public void AssetPanel_CanDisableArtworkWithoutDiscardingItsConfiguredAsset()
        {
            using DcduAssetPanel panel = new()
            {
                AssetFileName = "DCDU_Main_V15.png",
                ShowArtwork = false
            };

            Assert.False(panel.ShowArtwork);
            Assert.Equal("DCDU_Main_V15.png", panel.AssetFileName);
        }
    }
}
