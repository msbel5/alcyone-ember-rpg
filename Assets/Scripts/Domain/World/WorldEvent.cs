using System;
using EmberCrpg.Domain.Core;

// Design note:
// WorldEvent is the Faz 1 PROCESS-box payload appended to WorldEventLog.
// Inputs: deterministic GameTime tick, WorldEventKind category, optional ActorId
// subject, optional SiteId locus, and a non-blank reason label. At least one of
// ActorId / SiteId must be non-empty so every event carries a subject; the kind
// rejects WorldEventKind.None and the reason rejects blank input.
// Outputs: immutable record consumed by WorldEventLog in a follow-up Faz 1 PR;
// no Unity, no I/O, no serialization concerns. Mirrors SiteRecord / FactionRecord
// defensive-constructor pattern so invariants are pinned at construction.
// Atom-map ref: DOCS/sprint-faz-1-atom-map.md WorldEvent log + ReasonTrace sub-area.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure record describing a Faz 1 world event by tick, kind, optional actor, optional site, and reason.</summary>
    public sealed class WorldEvent
    {
        public WorldEvent(GameTime tick, WorldEventKind kind, ActorId actorId, SiteId siteId, string reason)
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
        }

        public GameTime Tick { get; }
        public WorldEventKind Kind { get; }
        public ActorId ActorId { get; }
        public SiteId SiteId { get; }
        public string Reason { get; }
    }
}
