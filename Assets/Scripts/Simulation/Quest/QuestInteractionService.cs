using System;
using System.Collections.Generic;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Simulation.Quest
{
    /// <summary>Deterministic interaction commands for quests that require explicit NPC handoff.</summary>
    public sealed class QuestInteractionService
    {
        public const string ForgeIronIngotTopicId = "forge_work";
        private const string IronOre = "iron_ore";
        private const string Fuel = "fuel";
        private const string IronIngot = "iron_ingot";

        public IReadOnlyList<AskAboutTopic> BuildTopics(WorldState world, NpcSeedRecord npc)
        {
            if (!IsForgeQuestGiver(npc))
                return Array.Empty<AskAboutTopic>();

            var state = TryGetForgeState(world);
            if (state != null && state.IsComplete)
                return Array.Empty<AskAboutTopic>();

            return new[]
            {
                new AskAboutTopic(ForgeIronIngotTopicId, BuildLabel(state), BuildPreview(state, npc))
            };
        }

        public bool TrySelectTopic(
            WorldState world,
            ActorId actorId,
            NpcSeedRecord npc,
            string topicId,
            out string line)
        {
            line = string.Empty;
            if (!string.Equals(topicId, ForgeIronIngotTopicId, StringComparison.Ordinal))
                return false;
            if (world == null || !IsForgeQuestGiver(npc))
                return false;

            world.EnsureInvariants();
            var state = TryGetForgeState(world);
            if (state == null)
                return TryStartForgeQuest(world, actorId, npc, out line);

            if (state.IsComplete)
            {
                line = $"{npc.Name} nods toward the cooled ingot. \"That work is done. The town remembers.\"";
                return true;
            }

            if (!state.IsTaskTriggered(0))
            {
                line = $"{npc.Name} taps the workbench. \"Ore and fuel first. Bring me one forged iron ingot.\"";
                return true;
            }

            if (!world.PlayerInventory.TryRemoveStackable(IronIngot, 1))
            {
                line = $"{npc.Name} checks your pack. \"The ingot is the proof. Come back with one in hand.\"";
                return true;
            }

            state.SetCompleted(success: true);
            AppendQuestEvent(world, WorldEventKind.QuestCompleted, actorId, "quest_completed:Forge an Iron Ingot:success");
            line = $"{npc.Name} weighs the ingot, then wraps it in oiled cloth. \"Good. You can shape iron, and maybe more.\"";
            return true;
        }

        public static bool IsForgeQuestGiver(NpcSeedRecord npc)
        {
            return npc != null && (npc.Role == NpcRole.Blacksmith || npc.Role == NpcRole.Artisan);
        }

        private static bool TryStartForgeQuest(WorldState world, ActorId actorId, NpcSeedRecord npc, out string line)
        {
            var inventory = world.PlayerInventory ?? (world.PlayerInventory = new InventoryState(10));
            if (!CanAddMissingStacks(inventory, IronOre, Fuel))
            {
                line = $"{npc.Name} studies your pack. \"Make room first. I will not hand ore into a full bag.\"";
                return true;
            }

            EnsureInventoryContains(inventory, SeedItemId(actorId, 1UL), IronOre, "Iron Ore", 2);
            EnsureInventoryContains(inventory, SeedItemId(actorId, 2UL), Fuel, "Fuel", 1);

            var quest = QuestCatalog.ForgeIronIngot();
            world.Quests.Add(quest.Id, new QuestState(quest.Tasks.Count, world.Time));
            AppendQuestEvent(world, WorldEventKind.QuestStarted, actorId, "quest_started:Forge an Iron Ingot");

            line = $"{npc.Name} gives you ore and fuel. \"Use the furnace. Return with one iron ingot, not excuses.\"";
            return true;
        }

        private static QuestState TryGetForgeState(WorldState world)
        {
            if (world?.Quests == null)
                return null;
            return world.Quests.TryGet(QuestCatalog.ForgeIronIngotId, out var state) ? state : null;
        }

        private static string BuildLabel(QuestState state)
        {
            if (state == null)
                return "forge work";
            return state.IsTaskTriggered(0) ? "deliver the ingot" : "the iron job";
        }

        private static string BuildPreview(QuestState state, NpcSeedRecord npc)
        {
            if (state == null)
                return $"{npc.Name} has a small forge job for steady hands.";
            return state.IsTaskTriggered(0)
                ? $"{npc.Name} is waiting for the forged ingot."
                : $"{npc.Name} expects one iron ingot from the ore and fuel already given.";
        }

        private static bool CanAddMissingStacks(InventoryState inventory, params string[] templateIds)
        {
            var missing = 0;
            foreach (var templateId in templateIds)
            {
                if (!inventory.Contains(templateId))
                    missing++;
            }

            return inventory.Items.Count + missing <= inventory.Capacity;
        }

        private static void EnsureInventoryContains(
            InventoryState inventory,
            ItemId seedItemId,
            string templateId,
            string displayName,
            int requiredQuantity)
        {
            var existingQuantity = 0;
            foreach (var item in inventory.Items)
            {
                if (!item.IsEquipment && string.Equals(item.TemplateId, templateId, StringComparison.Ordinal))
                    existingQuantity += item.Quantity;
            }

            if (existingQuantity < requiredQuantity)
                inventory.TryAdd(new InventoryItem(seedItemId, templateId, displayName, requiredQuantity - existingQuantity));
        }

        private static ItemId SeedItemId(ActorId actorId, ulong offset)
        {
            var basis = actorId.IsEmpty ? 1UL : actorId.Value;
            return new ItemId(8_800_000UL + basis * 10UL + offset);
        }

        private static void AppendQuestEvent(WorldState world, WorldEventKind kind, ActorId actorId, string reason)
        {
            var subject = actorId;
            if (subject.IsEmpty && world.Actors.TryFirstByRole(EmberCrpg.Domain.Actors.ActorRole.Player, out var player))
                subject = player.Id;
            if (subject.IsEmpty)
                return;

            world.Events.Append(new WorldEvent(world.Time, kind, subject, default, reason));
        }
    }
}
