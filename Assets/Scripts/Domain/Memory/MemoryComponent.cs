using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Narrative;

namespace EmberCrpg.Domain.Memory
{
    /// <summary>
    /// Actor-local indexed set of <see cref="MemoryFact"/> rows. Insertion-order
    /// deterministic enumeration, query by topic, forget facts older than a
    /// given game time. Phase 9 Atom 5.
    /// </summary>
    public sealed class MemoryComponent
    {
        private readonly List<MemoryFact> _facts = new List<MemoryFact>();

        public MemoryComponent(ActorId ownerId)
        {
            if (ownerId.IsEmpty)
                throw new ArgumentException("MemoryComponent.OwnerId must be non-empty.", nameof(ownerId));
            OwnerId = ownerId;
        }

        public ActorId OwnerId { get; }

        public int Count => _facts.Count;

        /// <summary>Adds a fact. Owner mismatch throws.</summary>
        public void Add(MemoryFact fact)
        {
            if (!fact.Rememberer.Equals(OwnerId))
                throw new ArgumentException("MemoryFact.Rememberer must match this component's owner.", nameof(fact));
            _facts.Add(fact);
        }

        /// <summary>Returns facts about a given topic in insertion order.</summary>
        public IEnumerable<MemoryFact> Query(TopicId topic)
        {
            if (topic.IsEmpty) yield break;
            foreach (var fact in _facts)
            {
                if (fact.Topic.Equals(topic))
                    yield return fact;
            }
        }

        /// <summary>Returns the most recent fact about a topic, or null when none exists.</summary>
        public MemoryFact? MostRecent(TopicId topic)
        {
            if (topic.IsEmpty) return null;
            MemoryFact? latest = null;
            foreach (var fact in _facts)
            {
                if (!fact.Topic.Equals(topic))
                    continue;
                if (!latest.HasValue || fact.RecordedAt.TotalMinutes > latest.Value.RecordedAt.TotalMinutes)
                    latest = fact;
            }
            return latest;
        }

        /// <summary>Drops all facts older than the supplied cutoff. Returns count removed.</summary>
        public int Forget(GameTime olderThan)
        {
            var removed = _facts.RemoveAll(f => f.RecordedAt.TotalMinutes < olderThan.TotalMinutes);
            return removed;
        }

        /// <summary>All facts in insertion order.</summary>
        public IReadOnlyList<MemoryFact> Facts => _facts;
    }
}
