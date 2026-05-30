using System;
using EmberCrpg.Domain.Core;

// Design note:
// WorldEvent is the Phase 1 PROCESS-box payload appended to WorldEventLog.
// Inputs: deterministic GameTime tick, WorldEventKind category, optional ActorId
// subject, optional SiteId locus, a non-blank reason label, and an optional
// ReasonTrace causal chain. At least one of ActorId / SiteId must be non-empty
// so every event carries a subject; the kind rejects WorldEventKind.None and
// the reason rejects blank input.
// Outputs: immutable record consumed by WorldEventLog; no Unity, no I/O,
// no serialization concerns. Mirrors SiteRecord / FactionRecord
// defensive-constructor pattern so invariants are pinned at construction.
// Atom-map ref: docs/sprint-phase-1-atom-map.md WorldEvent log + ReasonTrace sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure record describing a Phase 1 world event by tick, kind, optional actor, optional site, reason, and optional causal trace.</summary>
    public sealed class WorldEvent
    {
        public WorldEvent(GameTime tick, WorldEventKind kind, ActorId actorId, SiteId siteId, string reason, ReasonTrace reasonTrace = null)
        {
            if (kind == WorldEventKind.None)
                throw new ArgumentException("WorldEventKind.None is reserved as the empty sentinel.", nameof(kind));
            if (actorId.IsEmpty && siteId.IsEmpty)
                throw new ArgumentException("WorldEvent requires at least one of actorId or siteId to be non-empty.", nameof(actorId));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("WorldEvent reason is required.", nameof(reason));

            Tick = tick;
            Kind = kind;
            ActorId = actorId;
            SiteId = siteId;
            Reason = reason;
            ReasonTrace = reasonTrace;
        }

        public GameTime Tick { get; }
        public WorldEventKind Kind { get; }
        public ActorId ActorId { get; }
        public SiteId SiteId { get; }
        public string Reason { get; }
        public ReasonTrace ReasonTrace { get; }
    }
}
