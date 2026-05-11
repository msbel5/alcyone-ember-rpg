using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// Design note:
// WorldEventLog is the Faz 1 PROCESS-box append-only chronicle over WorldEvent.
// Inputs: WorldEvent instances appended one at a time, each non-null.
// Outputs: deterministic, insertion-order read-only view over the appended
// events; immutable through the public surface (the snapshot exposed via
// Events is wrapped in ReadOnlyCollection so callers cannot downcast to a
// mutable list). No Unity, no I/O, no serialization concerns. Mirrors the
// ActorStore / SiteStore / ItemStore / FactionStore defensive-constructor
// pattern: invariants pinned at append, no silent nulls accepted.
// Atom-map ref: DOCS/sprint-faz-1-atom-map.md WorldEvent log + ReasonTrace sub-area.
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

        public WorldEventLog()
        {
            _eventsView = new ReadOnlyCollection<WorldEvent>(_events);
        }

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

            _events.Add(worldEvent);
        }

        /// <summary>
        /// Read-only snapshot of the appended events in deterministic
        /// insertion order. The view tracks subsequent appends but cannot
        /// be downcast back to a mutable list.
        /// </summary>
        public IReadOnlyList<WorldEvent> Events
        {
            get { return _eventsView; }
        }
    }
}
