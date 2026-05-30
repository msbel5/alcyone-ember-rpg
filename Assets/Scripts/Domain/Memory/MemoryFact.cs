using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Narrative;

namespace EmberCrpg.Domain.Memory
{
    /// <summary>
    /// Immutable atomic fact remembered by one actor about another. Each fact
    /// carries a topic, an object actor (the subject of the fact), the game
    /// time it was recorded, and a stable detail string. Phase 9 Atom 4.
    /// </summary>
    public readonly struct MemoryFact : IEquatable<MemoryFact>
    {
        public MemoryFact(ActorId rememberer, TopicId topic, ActorId aboutActor, GameTime recordedAt, string detail)
        {
            if (rememberer.IsEmpty)
                throw new ArgumentException("MemoryFact.Rememberer must be non-empty.", nameof(rememberer));
            if (topic.IsEmpty)
                throw new ArgumentException("MemoryFact.Topic must be non-empty.", nameof(topic));

            Rememberer = rememberer;
            Topic = topic;
            AboutActor = aboutActor;
            RecordedAt = recordedAt;
            Detail = detail ?? string.Empty;
        }

        public ActorId Rememberer { get; }
        public TopicId Topic { get; }
        public ActorId AboutActor { get; }
        public GameTime RecordedAt { get; }
        public string Detail { get; }

        public bool Equals(MemoryFact other)
        {
            return Rememberer.Equals(other.Rememberer)
                && Topic.Equals(other.Topic)
                && AboutActor.Equals(other.AboutActor)
                && RecordedAt.Equals(other.RecordedAt)
                && string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is MemoryFact other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) ^ Rememberer.GetHashCode();
                hash = (hash * 31) ^ Topic.GetHashCode();
                hash = (hash * 31) ^ AboutActor.GetHashCode();
                hash = (hash * 31) ^ RecordedAt.GetHashCode();
                hash = (hash * 31) ^ (Detail?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
