using System;
using System.Collections.Generic;

// Design note:
// W32 EAT slice (docs/ruh/w32/04-action-log.md §2.2): the bounded deterministic phase-trace
// ring living on WorldState. CONSTRAINT: capacity is FIXED and content is deterministic —
// same seed + same ticks => identical ring. Push is O(1) and allocation-free after warm-up.
// TotalPushed is a monotone counter so an entry keeps its "nth transition" identity after wrap.
namespace EmberCrpg.Domain.Actors.Actions
{
    /// <summary>Bounded ring of the most recent action phase transitions.</summary>
    public sealed class ActionLogRing
    {
        public const int Capacity = 1024;

        private readonly ActionLogEntry[] _entries = new ActionLogEntry[Capacity];
        private int _start;

        public long TotalPushed { get; private set; }

        public int Count => TotalPushed >= Capacity ? Capacity : (int)TotalPushed;

        public void Push(in ActionLogEntry entry)
        {
            var index = (int)((_start + Count) % Capacity);
            if (Count == Capacity)
            {
                index = _start;
                _start = (_start + 1) % Capacity;
            }
            _entries[index] = entry;
            TotalPushed++;
        }

        /// <summary>Entry by age; 0 = oldest retained.</summary>
        public ActionLogEntry At(int indexFromOldest)
        {
            if (indexFromOldest < 0 || indexFromOldest >= Count)
                throw new ArgumentOutOfRangeException(nameof(indexFromOldest));
            return _entries[(_start + indexFromOldest) % Capacity];
        }

        /// <summary>Load path: rebuild the ring oldest-to-newest and restore the monotone counter.</summary>
        public void Restore(IReadOnlyList<ActionLogEntry> oldestToNewest, long totalPushed)
        {
            _start = 0;
            TotalPushed = 0;
            var count = oldestToNewest?.Count ?? 0;
            for (var i = count > Capacity ? count - Capacity : 0; i < count; i++)
                Push(oldestToNewest[i]);
            // Wrap identity survives the roundtrip; a corrupt counter that claims more history
            // than a FULL ring could legally shed is ignored (Count stays entry-backed).
            if (totalPushed > TotalPushed && Count == Capacity)
                TotalPushed = totalPushed;
        }
    }
}
