using System;
using Xunit;

namespace EasyCPDLC.Tests
{
    public sealed class HoppiePollingPolicyTests
    {
        [Fact]
        public void NextDelay_AlwaysUsesHoppiesRecommendedWindow()
        {
            for (int sample = 0; sample < 500; sample++)
            {
                TimeSpan delay = HoppiePollingPolicy.NextDelay();

                Assert.InRange(
                    delay.TotalSeconds,
                    HoppiePollingPolicy.MinimumSeconds,
                    HoppiePollingPolicy.MaximumSeconds);
            }
        }
    }
}
