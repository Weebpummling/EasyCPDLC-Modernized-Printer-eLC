using System;

namespace EasyCPDLC
{
    internal static class HoppiePollingPolicy
    {
        // Hoppie's technical guidance asks clients to use a randomized
        // 45-to-75-second interval. Keeping this policy in one place prevents
        // a future aircraft adapter from accidentally creating a second poller.
        internal const int MinimumSeconds = 45;
        internal const int MaximumSeconds = 75;

        internal static TimeSpan NextDelay()
        {
            return TimeSpan.FromSeconds(Random.Shared.Next(MinimumSeconds, MaximumSeconds + 1));
        }
    }
}
