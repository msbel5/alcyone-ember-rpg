using System.Linq;
using EmberCrpg.Domain.DM;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.DM;

// Design note:
// AskDmService is the slice's deterministic narrator shell over the typed DM query surface.
// Inputs: world state plus a player question string.
// Outputs: grounded summary text derived only from current simulation state and NPC memory.
// Bible reference: ARCHITECTURE.md DM query surface, PRD FR-07, Sprint 3 query-tier slice.
namespace EmberCrpg.Simulation.Narrative
{
    /// <summary>Deterministic Ask DM shell for the slice.</summary>
    public sealed class AskDmService
    {
        private readonly IDmQueryService _queries = new SliceDmQueryService();

        public string Ask(SliceWorldState world, string question)
        {
            var state = _queries.GetWorldState(world);
            var focus = _queries.GetRelevantNpcMemory(world, question);
            var doorState = state.DoorOpen
                ? "open"
                : state.GuardDoorAccessGranted ? "closed but cleared" : "blocked";
            var memory = focus.HasMemory
                ? string.Join(" | ", focus.RecentEvents)
                : "no notable memory recorded yet";
            var topics = focus.KnownTopics.Length == 0
                ? "none"
                : string.Join(", ", focus.KnownTopics.OrderBy(topicId => topicId));
            var enemyState = state.EnemyAlive ? "alive" : "down";
            return "DM tier 1: room seed " + state.RoomSeed
                + ", objective=" + state.RecommendedObjective
                + ", south door=" + doorState
                + ", enemy=" + enemyState
                + ", inventory " + state.InventorySlotsUsed + "/" + state.InventoryCapacity
                + ", focus=" + focus.NpcName
                + ", memory=" + memory
                + ", seen topics=" + topics
                + ", question='" + question + "'.";
        }
    }
}
