using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Inventory;

// Design note:
// GuardInteractionService gives Sentinel Rook a distinct deterministic checkpoint surface.
// Inputs: world state, player position, player inventory, and prior guard-warning state.
// Outputs: stateful guard responses plus optional door-clearance permission.
// Bible reference: ARCHITECTURE.md social/composure examples, PRD Sprint 2 FR-04.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Stateful guard interaction for warnings and passage clearance.</summary>
    public sealed class GuardInteractionService
    {
        public string Interact(SliceWorldState world)
        {
            if (world.Player.Position.ManhattanDistanceTo(world.Guard.Position) > 2)
                return "Stand closer to Sentinel Rook before asking for passage.";

            var memory = world.NpcMemory.GetOrCreate(world.Guard.Id);
            memory.RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.PassageRequested,
                world.Player.Id,
                "south_door",
                string.Empty,
                world.GuardWarningCount,
                world.Guard.Position));

            if (world.GuardDoorAccessGranted)
                return world.DoorOpen
                    ? "Sentinel Rook keeps his spear low. The south door is already clear."
                    : "Sentinel Rook nods once. Your clearance still stands for the south door.";
            if (world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId))
            {
                world.GuardDoorAccessGranted = true;
                memory.RecordEvent(new InteractionEvent(
                    world.Time,
                    ActorMemoryEventTypes.ClearanceGranted,
                    world.Player.Id,
                    "south_door",
                    SliceItemCatalog.GateWritTemplateId,
                    1,
                    world.Guard.Position));
                return "Sentinel Rook checks the gate writ, marks your face, and grants clearance for the south door.";
            }

            world.GuardWarningCount += 1;
            return world.GuardWarningCount == 1
                ? "Sentinel Rook bars the way. No writ, no hand on the south door."
                : world.GuardWarningCount == 2
                    ? "Sentinel Rook's gaze hardens. Final warning: bring a writ from Quartermaster Ivo."
                    : "Sentinel Rook plants his spear across the threshold. The post is closed to you.";
        }
    }
}
