using System;
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
        private const string SouthDoorPassageId = "south_door";
        private readonly NpcMemoryQueryService _memoryQueries = new NpcMemoryQueryService();

        public string Interact(SliceWorldState world)
        {
            if (world.Player.Position.ManhattanDistanceTo(world.Guard.Position) > 2)
                return "Stand closer to Sentinel Rook before asking for passage.";

            var context = _memoryQueries.GetGuardContext(world.NpcMemory, world.Guard.Id, SouthDoorPassageId);
            var memory = world.NpcMemory.GetOrCreate(world.Guard.Id);
            memory.RecordEvent(new InteractionEvent(
                world.Time,
                ActorMemoryEventTypes.PassageRequested,
                world.Player.Id,
                SouthDoorPassageId,
                string.Empty,
                context.PassageRequestCount,
                world.Guard.Position));

            if (world.GuardDoorAccessGranted || context.ClearanceGranted)
            {
                // PR#4 bot review fix: when context.ClearanceGranted is true but
                // world.GuardDoorAccessGranted is false (exactly the
                // persisted-memory-but-lost-flag scenario this path is meant to
                // recover), narrate AND mirror the memory back onto the world
                // flag so subsequent ticks treat the guard as already cleared
                // instead of re-walking the gate-writ branch.
                if (context.ClearanceGranted && !world.GuardDoorAccessGranted)
                    world.GuardDoorAccessGranted = true;

                return world.DoorOpen
                    ? "Sentinel Rook keeps his spear low. The south door is already clear."
                    : "Sentinel Rook nods once. Your clearance still stands for the south door.";
            }
            if (world.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId))
            {
                world.GuardDoorAccessGranted = true;
                memory.RecordEvent(new InteractionEvent(
                    world.Time,
                    ActorMemoryEventTypes.ClearanceGranted,
                    world.Player.Id,
                    SouthDoorPassageId,
                    SliceItemCatalog.GateWritTemplateId,
                    1,
                    world.Guard.Position));
                return "Sentinel Rook checks the gate writ, marks your face, and grants clearance for the south door.";
            }

            world.GuardWarningCount = Math.Max(world.GuardWarningCount, context.PassageRequestCount + 1);
            switch (context.Stance)
            {
                case GuardStance.InitialChallenge:
                    return "Sentinel Rook bars the way. No writ, no hand on the south door.";
                case GuardStance.FinalWarning:
                    return "Sentinel Rook remembers your first unwrit request; his gaze hardens. Final warning: bring a writ from Quartermaster Ivo.";
                default:
                    return $"Sentinel Rook plants his spear across the threshold after {context.PassageRequestCount + 1} requests. The post is closed to you.";
            }
        }
    }
}
