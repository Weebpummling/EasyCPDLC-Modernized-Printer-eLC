using EasyCPDLC;
using System.Drawing;
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
    }
}
