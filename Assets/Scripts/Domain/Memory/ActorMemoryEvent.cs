using EmberCrpg.Domain.Core;

// Design note:
// ActorMemoryEvent is the smallest persistent NPC-memory fact record for Sprint 3.
// Inputs: event time, type, related actor/item handles, quantity, topic, and note.
// Outputs: immutable pure-Domain memory facts suitable for save/load and DM queries.
// Bible reference: ARCHITECTURE.md §1.4 ActorMemory, §4 save model.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>Deterministic event types remembered by slice NPCs.</summary>
    public enum ActorMemoryEventType
    {
        DialogueTopic = 1,
        TradeCompleted = 2,
        CheckpointWarning = 3,
        DoorClearanceGranted = 4,
    }

    /// <summary>Immutable event entry remembered by one NPC.</summary>
    public sealed class ActorMemoryEvent
    {
        public ActorMemoryEvent(
            GameTime time,
            ActorMemoryEventType type,
            ActorId actorSeen,
            ItemId itemId,
            int amount,
            string topicId,
            string note)
        {
            Time = time;
            Type = type;
            ActorSeen = actorSeen;
            ItemId = itemId;
            Amount = amount;
            TopicId = topicId ?? string.Empty;
            Note = note ?? string.Empty;
        }

        public GameTime Time { get; }
        public ActorMemoryEventType Type { get; }
        public ActorId ActorSeen { get; }
        public ItemId ItemId { get; }
        public int Amount { get; }
        public string TopicId { get; }
        public string Note { get; }
    }
}
