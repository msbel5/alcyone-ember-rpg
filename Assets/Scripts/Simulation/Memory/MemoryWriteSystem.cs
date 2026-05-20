using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Memory
{
    /// <summary>
    /// Records memory facts about observed events. Pure: callers translate
    /// world events into Record calls. Faz 9 Atom 10.
    /// </summary>
    public sealed class MemoryWriteSystem
    {
        public void Record(
            MemoryComponent witnessMemory,
            TopicId topic,
            ActorId aboutActor,
            GameTime now,
            string detail)
        {
            if (witnessMemory == null) throw new ArgumentNullException(nameof(witnessMemory));
            if (topic.IsEmpty) throw new ArgumentException("Topic must be non-empty.", nameof(topic));
            witnessMemory.Add(new MemoryFact(witnessMemory.OwnerId, topic, aboutActor, now, detail ?? string.Empty));
        }

        public void RecordCrime(MemoryComponent witnessMemory, ActorId perpetrator, GameTime now, string detail)
        {
            Record(witnessMemory, new TopicId("crime"), perpetrator, now, detail);
        }

        public void RecordTrade(MemoryComponent witnessMemory, ActorId counterparty, GameTime now, string detail)
        {
            Record(witnessMemory, new TopicId("trade"), counterparty, now, detail);
        }

        public bool RecordFromWorldEvent(MemoryComponent witnessMemory, WorldEvent worldEvent)
        {
            if (witnessMemory == null) throw new ArgumentNullException(nameof(witnessMemory));
            if (worldEvent == null) throw new ArgumentNullException(nameof(worldEvent));

            var reason = worldEvent.Reason ?? string.Empty;
            if (reason.IndexOf("theft", StringComparison.OrdinalIgnoreCase) >= 0
                || reason.IndexOf("stole", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RecordCrime(witnessMemory, worldEvent.ActorId, worldEvent.Tick, reason);
                return true;
            }

            if (worldEvent.Kind == WorldEventKind.TradeCompleted)
            {
                RecordTrade(witnessMemory, worldEvent.ActorId, worldEvent.Tick, reason);
                return true;
            }

            return false;
        }
    }
}
