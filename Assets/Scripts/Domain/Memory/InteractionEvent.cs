using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

// Design note:
// InteractionEvent is one deterministic entry in an NPC's persistent memory log.
// Inputs: game timestamp, event type, involved actor/item/topic ids, amount, and room location.
// Outputs: immutable mechanical fact data; flavor systems may read it but do not own it.
// Bible reference: ARCHITECTURE.md ActorMemory.events ring buffer.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>Saved mechanical fact about a player/NPC interaction.</summary>
    public readonly struct InteractionEvent
    {
        public InteractionEvent(
            GameTime timestamp,
            string eventType,
            ActorId actorSeen,
            string subjectId,
            string itemTemplateId,
            int amount,
            GridPosition location)
        {
            Timestamp = timestamp;
            EventType = eventType ?? string.Empty;
            ActorSeen = actorSeen;
            SubjectId = subjectId ?? string.Empty;
            ItemTemplateId = itemTemplateId ?? string.Empty;
            Amount = amount;
            Location = location;
        }

        public GameTime Timestamp { get; }
        public string EventType { get; }
        public ActorId ActorSeen { get; }
        public string SubjectId { get; }
        public string ItemTemplateId { get; }
        public int Amount { get; }
        public GridPosition Location { get; }
    }
}
