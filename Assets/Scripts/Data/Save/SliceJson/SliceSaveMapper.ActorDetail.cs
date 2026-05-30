// SliceSaveMapper partial — actor detail: equipment, npc memory, interactions, transactions, inventory (split from the 961-line monolith, NAME/LOC-split).
using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Data.Save
{
    public static partial class SliceSaveMapper
    {
        private static EquipmentSaveData ToEquipmentData(EquipmentState equipment)
        {
            // Codex audit (A/P2): the previous version hardcoded
            // `new[] { EquipmentSlot.Weapon }`, so any future slot (armor,
            // shield, ring, etc.) would be silently dropped at save time.
            // Use the new stable enumerator on EquipmentState so every
            // non-empty equipped slot makes it into the DTO, in canonical
            // slot-code order for deterministic JSON output.
            return new EquipmentSaveData
            {
                slots = equipment.EnumerateEquipped()
                    .Select(pair => new EquippedItemSaveData
                    {
                        slot = (int)pair.Key,
                        slotCode = pair.Key.Code,
                        itemId = (long)pair.Value.Value,
                    })
                    .ToArray(),
            };
        }

        private static EquipmentState ToEquipmentState(EquipmentSaveData data)
        {
            var equipment = new EquipmentState();
            foreach (var slot in data?.slots ?? Array.Empty<EquippedItemSaveData>())
                equipment.Equip(ToEquipmentSlot(slot.slotCode, slot.slot), new ItemId((ulong)slot.itemId));
            return equipment;
        }

        private static EquipmentSlot ToEquipmentSlot(string code, int legacyValue)
        {
            return string.IsNullOrWhiteSpace(code)
                ? EquipmentSlot.FromLegacyValue(legacyValue)
                : EquipmentSlot.FromCode(code);
        }

        private static NpcMemorySaveData[] ToNpcMemoryData(NpcMemoryStore store)
        {
            return (store ?? new NpcMemoryStore()).GetAllSorted().Select(memory => new NpcMemorySaveData
            {
                actorId = (long)memory.ActorId.Value,
                events = memory.Events.Select(ToInteractionEventData).ToArray(),
                dialogueSeen = memory.DialogueSeen.OrderBy(topicId => topicId).ToArray(),
                transactions = memory.Transactions.Select(ToTransactionData).ToArray(),
            }).ToArray();
        }

        private static NpcMemoryStore ToNpcMemoryStore(NpcMemorySaveData[] data)
        {
            var store = new NpcMemoryStore();
            store.ReplaceAll((data ?? Array.Empty<NpcMemorySaveData>()).Select(ToActorMemory));
            return store;
        }

        private static ActorMemory ToActorMemory(NpcMemorySaveData data)
        {
            var memory = new ActorMemory(new ActorId((ulong)data.actorId));
            memory.ReplaceEvents((data.events ?? Array.Empty<InteractionEventSaveData>()).Select(ToInteractionEvent));
            memory.ReplaceDialogueSeen(data.dialogueSeen);
            memory.ReplaceTransactions((data.transactions ?? Array.Empty<TransactionSaveData>()).Select(ToTransaction));
            return memory;
        }

        private static InteractionEventSaveData ToInteractionEventData(InteractionEvent interactionEvent)
        {
            return new InteractionEventSaveData
            {
                timestampMinutes = (long)interactionEvent.Timestamp.TotalMinutes,
                eventType = interactionEvent.EventType,
                actorSeen = (long)interactionEvent.ActorSeen.Value,
                subjectId = interactionEvent.SubjectId,
                itemTemplateId = interactionEvent.ItemTemplateId,
                amount = interactionEvent.Amount,
                locationX = interactionEvent.Location.X,
                locationY = interactionEvent.Location.Y,
            };
        }

        private static InteractionEvent ToInteractionEvent(InteractionEventSaveData data)
        {
            return new InteractionEvent(
                new GameTime(data.timestampMinutes),
                data.eventType,
                new ActorId((ulong)data.actorSeen),
                data.subjectId,
                data.itemTemplateId,
                data.amount,
                new GridPosition(data.locationX, data.locationY));
        }

        private static TransactionSaveData ToTransactionData(TransactionRecord transaction)
        {
            return new TransactionSaveData
            {
                timestampMinutes = (long)transaction.Timestamp.TotalMinutes,
                transactionType = transaction.TransactionType,
                itemTemplateId = transaction.ItemTemplateId,
                count = transaction.Count,
                goldDelta = transaction.GoldDelta,
            };
        }

        private static TransactionRecord ToTransaction(TransactionSaveData data)
        {
            return new TransactionRecord(
                new GameTime(data.timestampMinutes),
                data.transactionType,
                data.itemTemplateId,
                data.count,
                data.goldDelta);
        }

        private static InventorySaveData ToInventoryData(InventoryState inventory)
        {
            return new InventorySaveData
            {
                capacity = inventory.Capacity,
                items = inventory.Items.Select(ItemSaveMapper.ToData).ToArray(),
            };
        }

        private static InventoryState ToInventoryState(InventorySaveData inventory, int fallbackCapacity)
        {
            var state = new InventoryState(inventory?.capacity ?? fallbackCapacity);
            foreach (var item in inventory?.items ?? Array.Empty<ItemSaveData>())
                state.TryAdd(ItemSaveMapper.ToItem(item));
            return state;
        }
    }
}
