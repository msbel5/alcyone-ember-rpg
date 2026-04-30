using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;

// Design note:
// MemorySaveMapper isolates NPC-memory save/load translation for Sprint 3.
// Inputs: ActorMemory records and memory DTOs.
// Outputs: round-trippable persistent memory snapshots for save/load and DM queries.
// Bible reference: ARCHITECTURE.md §1.4 ActorMemory, §4 save model.
namespace EmberCrpg.Data.Save
{
    /// <summary>Field mapper between NPC-memory records and save DTOs.</summary>
    public static class MemorySaveMapper
    {
        public static NpcMemorySaveData ToData(ActorMemory memory)
        {
            return new NpcMemorySaveData
            {
                ownerId = memory.OwnerId.Value,
                dialogueSeen = memory.DialogueSeen.ToArray(),
                events = memory.Events.Select(ToData).ToArray(),
            };
        }

        public static ActorMemory ToMemory(NpcMemorySaveData memory)
        {
            var actorMemory = new ActorMemory(new ActorId(memory.ownerId));
            actorMemory.Replace((memory.events ?? new ActorMemoryEventSaveData[0]).Select(ToEvent), memory.dialogueSeen);
            return actorMemory;
        }

        private static ActorMemoryEventSaveData ToData(ActorMemoryEvent entry)
        {
            return new ActorMemoryEventSaveData
            {
                totalMinutes = entry.Time.TotalMinutes,
                type = (int)entry.Type,
                actorSeenId = entry.ActorSeen.Value,
                itemId = entry.ItemId.Value,
                amount = entry.Amount,
                topicId = entry.TopicId,
                note = entry.Note,
            };
        }

        private static ActorMemoryEvent ToEvent(ActorMemoryEventSaveData entry)
        {
            return new ActorMemoryEvent(
                new GameTime(entry.totalMinutes),
                (ActorMemoryEventType)entry.type,
                new ActorId(entry.actorSeenId),
                new ItemId(entry.itemId),
                entry.amount,
                entry.topicId,
                entry.note);
        }
    }
}
