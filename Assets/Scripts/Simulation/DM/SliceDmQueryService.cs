using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.DM;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;

// Design note:
// SliceDmQueryService turns slice world state plus NPC memory into typed Tier 1-2 DM answers.
// Inputs: pure world state and a player question string for deterministic focus selection.
// Outputs: grounded current-state and inspection views with no live AI dependency.
// Bible reference: ARCHITECTURE.md DM API Tier 1, ActorMemory, Sprint 3 query-surface slice.
namespace EmberCrpg.Simulation.DM
{
    /// <summary>Deterministic DM query implementation for the vertical slice.</summary>
    public sealed class SliceDmQueryService : IDmQueryService
    {
        public DmWorldStateView GetWorldState(SliceWorldState world)
        {
            var objective = world.GuardDoorAccessGranted
                ? world.DoorOpen
                    ? "Cross the south door or save the room while the checkpoint is clear."
                    : "Sentinel Rook has cleared you; open the south door when ready."
                : world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId)
                    ? "Show the sealed gate writ to Sentinel Rook."
                    : world.PlayerInventory.Contains(SliceItemCatalog.EmberShardTemplateId)
                        ? "Trade the Ember Shard with Quartermaster Ivo for a gate writ."
                        : world.Pickups.Any(pickup => !pickup.IsCollected)
                            ? "Pick up the Ember Shard before approaching the checkpoint."
                            : world.Enemy.IsAlive
                                ? "The Ash Rat is still the only live threat in the room."
                                : "Save the room state and move onward.";

            return new DmWorldStateView(
                world.RoomSeed,
                world.Enemy.IsAlive,
                world.DoorOpen,
                world.GuardDoorAccessGranted,
                world.PlayerInventory.Items.Count,
                world.PlayerInventory.Capacity,
                objective);
        }

        public DmInspectionView GetInspection(SliceWorldState world, string question)
        {
            var focus = ResolveFocusActor(world, question);
            var reason = ResolveFocusReason(world, question, focus);
            var weapon = world.PlayerEquipment == null || world.PlayerEquipment.Weapon == null ? "none" : world.PlayerEquipment.Weapon.DisplayName;
            var armor = world.PlayerEquipment == null || world.PlayerEquipment.Armor == null ? "none" : world.PlayerEquipment.Armor.DisplayName;
            return new DmInspectionView(
                world.Room.LayoutId.ToString(),
                GuardInteractionService.GetAttitudeLabel(world),
                GuardInteractionService.GetWatchReputation(world),
                weapon,
                armor,
                world.Pickups.Count(pickup => !pickup.IsCollected),
                reason);
        }

        public DmNpcMemoryView GetNpcMemory(SliceWorldState world, EmberCrpg.Domain.Core.ActorId npcId)
        {
            var actor = ResolveActor(world, npcId);
            ActorMemory memory = null;
            var hasMemory = world.NpcMemories != null && world.NpcMemories.TryGet(npcId, out memory);
            if (!hasMemory)
                return new DmNpcMemoryView(actor.Id, actor.Name, new string[0], new string[0]);

            var recentEvents = memory.Events
                .Reverse()
                .Take(3)
                .Select(FormatEvent)
                .ToArray();
            var knownTopics = memory.DialogueSeen
                .OrderBy(topicId => topicId, StringComparer.Ordinal)
                .ToArray();
            return new DmNpcMemoryView(actor.Id, actor.Name, recentEvents, knownTopics);
        }

        public DmNpcMemoryView GetRelevantNpcMemory(SliceWorldState world, string question)
        {
            return GetNpcMemory(world, ResolveFocusActor(world, question).Id);
        }

        private static ActorRecord ResolveFocusActor(SliceWorldState world, string question)
        {
            var text = (question ?? string.Empty).ToLowerInvariant();
            if (ContainsAny(text, "guard", "rook", "door", "clearance", "pass"))
                return world.Guard;
            if (ContainsAny(text, "merchant", "ivo", "trade", "writ", "quartermaster"))
                return world.Merchant;
            if (ContainsAny(text, "talk", "topic", "sage", "nera", "ask"))
                return world.Talker;

            var mostRecent = world.NpcMemories == null
                ? null
                : world.NpcMemories.Entries
                    .SelectMany(memory => memory.Events.Select(entry => new { memory, entry }))
                    .OrderByDescending(pair => pair.entry.Time.TotalMinutes)
                    .FirstOrDefault();
            if (mostRecent != null)
                return ResolveActor(world, mostRecent.memory.OwnerId);

            if (world.PlayerInventory.Contains(SliceItemCatalog.EmberShardTemplateId))
                return world.Merchant;
            if (world.GuardDoorAccessGranted || world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId))
                return world.Guard;
            return world.Talker;
        }

        private static string ResolveFocusReason(SliceWorldState world, string question, ActorRecord focus)
        {
            var text = (question ?? string.Empty).ToLowerInvariant();
            if (focus.Id == world.Guard.Id && ContainsAny(text, "guard", "rook", "door", "clearance", "pass"))
                return "question keywords point at the checkpoint";
            if (focus.Id == world.Merchant.Id && ContainsAny(text, "merchant", "ivo", "trade", "writ", "quartermaster"))
                return "question keywords point at the merchant loop";
            if (focus.Id == world.Talker.Id && ContainsAny(text, "talk", "topic", "sage", "nera", "ask"))
                return "question keywords point at Sage Nera";
            if (world.NpcMemories != null && world.NpcMemories.Entries.Any(memory => memory.OwnerId == focus.Id && memory.Events.Count > 0))
                return "most recent memory belongs to " + focus.Name;
            if (focus.Id == world.Merchant.Id && world.PlayerInventory.Contains(SliceItemCatalog.EmberShardTemplateId))
                return "inventory suggests a pending trade";
            if (focus.Id == world.Guard.Id && (world.GuardDoorAccessGranted || world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId)))
                return "checkpoint state is the next deterministic gate";
            return "default slice focus";
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }

        private static ActorRecord ResolveActor(SliceWorldState world, EmberCrpg.Domain.Core.ActorId npcId)
        {
            if (world.Talker.Id == npcId)
                return world.Talker;
            if (world.Merchant.Id == npcId)
                return world.Merchant;
            if (world.Guard.Id == npcId)
                return world.Guard;
            return world.Talker;
        }

        private static string FormatEvent(ActorMemoryEvent entry)
        {
            switch (entry.Type)
            {
                case ActorMemoryEventType.DialogueTopic:
                    return "Discussed topic '" + entry.TopicId + "'.";
                case ActorMemoryEventType.TradeCompleted:
                    return string.IsNullOrEmpty(entry.Note) ? "Completed a trade." : entry.Note;
                case ActorMemoryEventType.CheckpointWarning:
                    return string.IsNullOrEmpty(entry.Note) ? "Issued a checkpoint warning." : entry.Note;
                case ActorMemoryEventType.DoorClearanceGranted:
                    return string.IsNullOrEmpty(entry.Note) ? "Granted south-door clearance." : entry.Note;
                default:
                    return string.IsNullOrEmpty(entry.Note) ? "Recorded an event." : entry.Note;
            }
        }
    }
}
