using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyCPDLC
{
    internal sealed class DatalinkPrintDeduplicator
    {
        private readonly object sync = new();
        private readonly Dictionary<string, DateTime> seen = new(StringComparer.Ordinal);
        private readonly TimeSpan retention;
        private readonly int capacity;

        public DatalinkPrintDeduplicator(TimeSpan? retention = null, int capacity = 512)
        {
            this.retention = retention ?? TimeSpan.FromHours(6);
            this.capacity = Math.Max(16, capacity);
        }

        public bool TryRegister(string stableMessageId, DateTime utcNow)
        {
            string id = (stableMessageId ?? string.Empty).Trim();
            if (id.Length == 0)
            {
                return true;
            }

            lock (sync)
            {
                DateTime cutoff = utcNow - retention;
                foreach (string expired in seen.Where(item => item.Value < cutoff).Select(item => item.Key).ToArray())
                {
                    seen.Remove(expired);
                }

                if (seen.TryGetValue(id, out DateTime previous) && previous >= cutoff)
                {
                    return false;
                }

                seen[id] = utcNow;
                while (seen.Count > capacity)
                {
                    string oldest = seen.OrderBy(item => item.Value).First().Key;
                    seen.Remove(oldest);
                }
                return true;
            }
        }

        public void Forget(string stableMessageId)
        {
            string id = (stableMessageId ?? string.Empty).Trim();
            if (id.Length == 0)
            {
                return;
            }

            lock (sync)
            {
                seen.Remove(id);
            }
        }
    }
}
