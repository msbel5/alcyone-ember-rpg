using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;

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
    }
}
