using System.Linq;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;

// Design note:
// GuardInteractionService gives Sentinel Rook a distinct deterministic checkpoint surface.
// Inputs: world state, player position, player inventory, and prior guard-warning state.
// Outputs: stateful guard responses, persistent guard memory, and optional door-clearance permission.
// Bible reference: ARCHITECTURE.md social/composure examples, ActorMemory, PRD Sprint 2 FR-04, Sprint 3 memory slice.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Stateful guard interaction for warnings and passage clearance.</summary>
    public sealed class GuardInteractionService
    {
        public string Interact(SliceWorldState world)
        {
            if (world.Player.Position.ManhattanDistanceTo(world.Guard.Position) > 2)
                return "Stand closer to Sentinel Rook before asking for passage.";
            if (world.GuardDoorAccessGranted)
                return world.DoorOpen
                    ? "Sentinel Rook keeps his spear low. The south door is already clear."
                    : "Sentinel Rook nods once. Your clearance still stands for the south door.";
            if (world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId))
            {
                world.GuardDoorAccessGranted = true;
                Remember(world, ActorMemoryEventType.DoorClearanceGranted, SliceItemCatalog.GateWritTemplateId, "Sentinel Rook granted south-door clearance after checking a sealed gate writ.");
                return "Sentinel Rook checks the gate writ, marks your face, and grants clearance for the south door.";
            }

            world.GuardWarningCount += 1;
            Remember(world, ActorMemoryEventType.CheckpointWarning, string.Empty, "Sentinel Rook issued checkpoint warning #" + world.GuardWarningCount + ".");
            return world.GuardWarningCount == 1
                ? "Sentinel Rook bars the way. No writ, no hand on the south door."
                : world.GuardWarningCount == 2
                    ? "Sentinel Rook's gaze hardens. Final warning: bring a writ from Quartermaster Ivo."
                    : "Sentinel Rook plants his spear across the threshold. The post is closed to you.";
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
