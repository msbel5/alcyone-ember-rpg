using System.Linq;
using EmberCrpg.Domain.World;

// Design note:
// ThinkService offers deterministic inner-monologue guidance based on current slice state.
// Inputs: world snapshot only.
// Outputs: short rules-based thought text for UI and tests.
// Bible reference: PRD FR-07.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Rules-based Think shell for the vertical slice.</summary>
    public sealed class ThinkService
    {
        public string Think(SliceWorldState world)
        {
            if (world.Player.Vitals.Health.Current <= world.Player.Vitals.Health.Max / 2)
                return "Think: I'm hurt. Save or finish the rat before pushing deeper.";
            if (world.Pickups.Any(pickup => !pickup.IsCollected) && !world.PlayerInventory.IsFull)
                return "Think: The Ember Shard is still on the floor; grab it before leaving.";
            if (world.Enemy.IsAlive)
                return "Think: The rat is the only live threat left in this room.";
            return "Think: The room is stable enough to save and move on.";
        }
    }
}
