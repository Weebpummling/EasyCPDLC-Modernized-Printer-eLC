using EasyCPDLC;
using System;
using Xunit;

namespace EasyCPDLC.Tests
{
    public sealed class VersioningTests
    {
        [Theory]
        [InlineData("printer-elc-v1.1.0")]
        [InlineData("PRINTER-ELC-V2.3.4")]
        [InlineData("1.0.0.17")]
        public void ForkReleaseTags_AcceptNamespaceAndMigrationBaseline(string tag)
        {
            Assert.True(MainForm.IsForkReleaseTag(tag));
        }

        [Theory]
        [InlineData("1.0.0.18")]
        [InlineData("v1.1.0")]
        [InlineData("cpdlc1.1.0")]
        [InlineData("")]
        public void ForkReleaseTags_RejectUnnamespacedOrUpstreamTags(string tag)
        {
            Assert.False(MainForm.IsForkReleaseTag(tag));
        }

        [Fact]
        public void NamespacedVersion_ParsesForUpdaterComparison()
        {
            Assert.Equal(new Version(1, 1, 0, 0), MainForm.ParseReleaseVersion("printer-elc-v1.1.0"));
            Assert.True(
                MainForm.ParseReleaseVersion("printer-elc-v1.1.0") >
                MainForm.ParseReleaseVersion("1.0.0.17"));
        }

        [Fact]
        public void VersionBadge_IsForkBrandedAndDropsZeroRevision()
        {
            Assert.Equal("P/eLC 1.1.0", MainForm.FormatForkDisplayVersion("1.1.0.0"));
        }
    }
}
