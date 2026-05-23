using System;

// Design note:
// WorldHistoryEvent is the worldgen FOUNDATION's year-resolution historical
// event. Intentionally distinct from EmberCrpg.Domain.World.WorldEvent: that
// type carries a per-tick GameTime + ActorId/SiteId for the runtime
// WorldEventLog, while WorldHistoryEvent carries a year + free-form
// subject/detail strings so the 100-year backstory generated before play
// begins can name things that do not (yet) have stable Domain ids — long-dead
// kings, ruined cities, mythic battles. Both surfaces deliberately do NOT
// share a base type; merging them would force every runtime event to carry a
// subject string and every historical event to carry a GameTime tick.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>Pure record describing a year-resolution world-history event with kind, free-form subject, and free-form detail.</summary>
    public sealed class WorldHistoryEvent
    {
        public WorldHistoryEvent(int year, WorldHistoryKind kind, string subject, string detail)
        {
            if (kind == WorldHistoryKind.None)
                throw new ArgumentException("WorldHistoryKind.None is reserved as the empty sentinel.", nameof(kind));
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("WorldHistoryEvent subject is required.", nameof(subject));
            if (detail == null)
                throw new ArgumentNullException(nameof(detail));

            Year = year;
            Kind = kind;
            Subject = subject;
            Detail = detail;
        }

        public int Year { get; }
        public WorldHistoryKind Kind { get; }
        public string Subject { get; }
        public string Detail { get; }
    }
}
