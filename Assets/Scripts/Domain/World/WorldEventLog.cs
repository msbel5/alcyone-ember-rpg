using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// Design note:
// WorldEventLog is the Phase 1 PROCESS-box append-only chronicle over WorldEvent.
// Inputs: WorldEvent instances appended one at a time, each non-null.
// Outputs: deterministic, insertion-order read-only live view over the
// appended events; immutable through the public surface (the view exposed
// via Events is wrapped in ReadOnlyCollection so callers cannot downcast
// to a mutable list, but later Appends remain visible through it). No
// Unity, no I/O, no serialization concerns. Mirrors the
// ActorStore / SiteStore / ItemStore / FactionStore defensive-constructor
// pattern: invariants pinned at append, no silent nulls accepted.
// Atom-map ref: docs/sprint-phase-1-atom-map.md WorldEvent log + ReasonTrace sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// Append-only chronicle over <see cref="WorldEvent"/> preserving
    /// deterministic insertion order. Null events are rejected at append so
    /// downstream consumers can rely on every entry being a valid payload.
    /// </summary>
    public sealed class WorldEventLog
    {
        private readonly List<WorldEvent> _events = new List<WorldEvent>();
        private readonly ReadOnlyCollection<WorldEvent> _eventsView;
        // B21 (W32-04 §6): seq accounting for the bounded log. FirstRetainedSeq is the absolute
        // append-index of _events[0]; TotalAppended is the count of every event ever appended
        // (never decreases). Invariant: TotalAppended == FirstRetainedSeq + _events.Count.
        // Cursors persist as seq (long), not index — so a trim never re-mills or skips.
        private long _firstRetainedSeq;
        private long _totalAppended;

        public WorldEventLog()
        {
            _eventsView = new ReadOnlyCollection<WorldEvent>(_events);
        }

        /// <summary>
        /// Restore-ctor used by the save mapper: seeds the seq baseline for a log that will be
        /// filled with N post-trim events, so <c>TotalAppended</c> lands at
        /// <paramref name="firstRetainedSeq"/> + N once loading completes. Pre-trim saves pass 0.
        /// </summary>
        public WorldEventLog(long firstRetainedSeq) : this()
        {
            if (firstRetainedSeq < 0)
                throw new ArgumentOutOfRangeException(nameof(firstRetainedSeq));
            _firstRetainedSeq = firstRetainedSeq;
            _totalAppended = firstRetainedSeq;
        }

        /// <summary>Absolute append-seq of _events[0]; advances by N after a TrimOldest(N).</summary>
        public long FirstRetainedSeq { get { return _firstRetainedSeq; } }

        /// <summary>Monotone count of every event ever appended (survives trim).</summary>
        public long TotalAppended { get { return _totalAppended; } }

        /// <summary>Number of events currently appended.</summary>
        public int Count
        {
            get { return _events.Count; }
        }

        /// <summary>True when no events have been appended.</summary>
        public bool IsEmpty
        {
            get { return _events.Count == 0; }
        }

        /// <summary>
        /// Appends a world event to the chronicle. Throws when the event is
        /// null so the log never contains silent gaps.
        /// </summary>
        public void Append(WorldEvent worldEvent)
        {
            if (worldEvent == null)
                throw new ArgumentNullException(nameof(worldEvent));
            if (_totalAppended == long.MaxValue)
                throw new InvalidOperationException("World event sequence exhausted.");

            worldEvent.AssignSequence(_totalAppended);
            _events.Add(worldEvent);
            _totalAppended++;
        }

        /// <summary>
        /// B21 (W32-04 §6): drop the oldest events so <c>Count &lt;= maxRetained</c>. Advances
        /// <see cref="FirstRetainedSeq"/> by the drop count and leaves <see cref="TotalAppended"/>
        /// unchanged. Returns the number of rows dropped (0 when already at or below the cap).
        /// O(N) via <c>List.RemoveRange</c> — at MaxRetained=16384 a full trim is a ~64KB memmove
        /// once per game-day; switch to a ring buffer if MaxRetained grows past ~100k.
        /// </summary>
        public int TrimOldest(int maxRetained)
        {
            if (maxRetained < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetained));
            if (_events.Count <= maxRetained) return 0;
            int drop = _events.Count - maxRetained;
            _events.RemoveRange(0, drop);
            _firstRetainedSeq += drop;
            return drop;
        }

        /// <summary>
        /// B21: map an absolute append-seq to the current <see cref="Events"/> index. Seqs older
        /// than <see cref="FirstRetainedSeq"/> clamp to 0 (start of the retained window); seqs at
        /// or beyond <see cref="TotalAppended"/> clamp to <c>Events.Count</c> (nothing to consume).
        /// Always returns true — the out is a clamped valid index into <see cref="Events"/>.
        /// </summary>
        public bool TryIndexForSeq(long seq, out int index)
        {
            if (seq <= _firstRetainedSeq) { index = 0; return true; }
            if (seq >= _totalAppended) { index = _events.Count; return true; }
            index = (int)(seq - _firstRetainedSeq);
            return true;
        }

        /// <summary>
        /// Read-only live view of the appended events in deterministic
        /// insertion order. The view is not a point-in-time snapshot:
        /// it reflects subsequent <see cref="Append"/> calls so callers
        /// MUST NOT cache it as an immutable copy. The view cannot be
        /// downcast back to a mutable list.
        /// </summary>
        public IReadOnlyList<WorldEvent> Events
        {
            get { return _eventsView; }
        }
    }
}
