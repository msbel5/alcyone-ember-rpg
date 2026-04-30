using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;

// Design note:
// GuardInteractionService gives Sentinel Rook a distinct deterministic checkpoint surface.
// Inputs: world state, player position, player inventory, guard warning state, and city-watch reputation.
// Outputs: stateful guard responses, persistent guard memory, reputation hooks, and optional door-clearance permission.
// Bible reference: ARCHITECTURE.md social/composure examples, ActorMemory, PRD Sprint 2 FR-04, Sprint 3 memory + reputation slice.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Stateful guard interaction for warnings, reputation, and passage clearance.</summary>
    public sealed class GuardInteractionService
    {
        public const string CityWatchFactionId = "city_watch";

        public string Interact(SliceWorldState world)
        {
            if (world.Player.Position.ManhattanDistanceTo(world.Guard.Position) > 2)
                return "Stand closer to Sentinel Rook before asking for passage.";
            if (world.GuardDoorAccessGranted)
                return GetAttitudeLabel(world) == "respectful"
                    ? "Sentinel Rook keeps his spear low. Your clearance still stands, and the watch remembers the favor."
                    : world.DoorOpen
                        ? "Sentinel Rook keeps his spear low. The south door is already clear."
                        : "Sentinel Rook nods once. Your clearance still stands for the south door.";
            if (world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId))
            {
                world.GuardDoorAccessGranted = true;
                AdjustWatchReputation(world, 1);
                Remember(world, ActorMemoryEventType.DoorClearanceGranted, SliceItemCatalog.GateWritTemplateId, "Sentinel Rook granted south-door clearance after checking a sealed gate writ.");
                return "Sentinel Rook checks the gate writ, marks your face, and grants clearance for the south door.";
            }

            if (GetWatchReputation(world) >= 2 && world.GuardWarningCount == 0)
            {
                AdjustWatchReputation(world, -1);
                Remember(world, ActorMemoryEventType.CheckpointWarning, string.Empty, "Sentinel Rook honored prior watch standing and let one missing-writ approach pass as a reminder.");
                return "Sentinel Rook recognizes your prior standing and lets the first lapse pass. Next time, bring the writ.";
            }

            var warningStep = GetWatchReputation(world) <= -2 ? 2 : 1;
            world.GuardWarningCount += warningStep;
            AdjustWatchReputation(world, -1);
            Remember(world, ActorMemoryEventType.CheckpointWarning, string.Empty, "Sentinel Rook issued checkpoint warning #" + world.GuardWarningCount + ".");
            return world.GuardWarningCount == 1
                ? "Sentinel Rook bars the way. No writ, no hand on the south door."
                : world.GuardWarningCount == 2
                    ? "Sentinel Rook's gaze hardens. Final warning: bring a writ from Quartermaster Ivo."
                    : "Sentinel Rook plants his spear across the threshold. The post is closed to you.";
        }

        public static int GetWatchReputation(SliceWorldState world)
        {
            return world.Reputations == null ? 0 : world.Reputations.Get(CityWatchFactionId);
        }

        public static string GetAttitudeLabel(SliceWorldState world)
        {
            var reputation = GetWatchReputation(world);
            if (world.GuardDoorAccessGranted || reputation >= 2)
                return "respectful";
            if (reputation <= -2 || world.GuardWarningCount >= 3)
                return "hostile";
            if (world.GuardWarningCount > 0 || reputation < 0)
                return "wary";
            return "formal";
        }

        private static int AdjustWatchReputation(SliceWorldState world, int delta)
        {
            if (world.Reputations == null)
                world.Reputations = new FactionReputationLedger();

            return world.Reputations.Adjust(CityWatchFactionId, delta);
        }

        private static void Remember(SliceWorldState world, ActorMemoryEventType type, string templateId, string note)
        {
            var memory = world.NpcMemories == null ? null : world.NpcMemories.GetOrCreate(world.Guard.Id);
            if (memory == null)
                return;

            var itemId = ItemId.Empty;
            if (!string.IsNullOrEmpty(templateId))
            {
                var item = world.PlayerInventory.Items.FirstOrDefault(candidate => candidate.TemplateId == templateId);
                if (item != null)
                    itemId = item.Id;
            }

            memory.Remember(new ActorMemoryEvent(world.Time, type, world.Player.Id, itemId, 1, string.Empty, note));
        }
    }
}
