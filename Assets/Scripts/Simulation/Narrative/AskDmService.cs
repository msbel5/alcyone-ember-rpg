using EmberCrpg.Domain.World;

// Design note:
// AskDmService is Sprint 1's deterministic narrator shell, not a live LLM integration.
// Inputs: world state plus a player question string.
// Outputs: grounded summary text derived only from current simulation state.
// Bible reference: ARCHITECTURE.md DM query surface, PRD FR-07.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Deterministic Ask DM shell for the slice.</summary>
    public sealed class AskDmService
    {
        public string Ask(SliceWorldState world, string question)
        {
            var enemyState = world.Enemy.IsAlive ? "still stalking the north-east corner" : "already down";
            return $"DM shell: room seed {world.RoomSeed}, enemy is {enemyState}, inventory {world.PlayerInventory.Items.Count}/{world.PlayerInventory.Capacity}, question='{question}'.";
        }
    }
}
