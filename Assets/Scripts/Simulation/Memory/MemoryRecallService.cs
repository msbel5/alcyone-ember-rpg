using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;

namespace EmberCrpg.Simulation.Memory
{
    /// <summary>
    /// Recalls memory facts for a given topic with optional recency filtering.
    /// Faz 9 Atom 7.
    /// </summary>
    public sealed class MemoryRecallService
    {
        public IReadOnlyList<MemoryFact> Recall(MemoryComponent component, TopicId topic, GameTime since)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (topic.IsEmpty) return new MemoryFact[0];

            var results = new List<MemoryFact>();
            foreach (var fact in component.Facts)
            {
                if (!fact.Topic.Equals(topic)) continue;
                if (fact.RecordedAt.TotalMinutes < since.TotalMinutes) continue;
                results.Add(fact);
            }
            return results;
        }

        public bool HasRecentFact(MemoryComponent component, TopicId topic, GameTime since)
        {
            if (component == null || topic.IsEmpty) return false;
            foreach (var fact in component.Facts)
            {
                if (fact.Topic.Equals(topic) && fact.RecordedAt.TotalMinutes >= since.TotalMinutes)
                    return true;
            }
            return false;
        }
    }
}
