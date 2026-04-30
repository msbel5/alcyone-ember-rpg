using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// IDmQueryService defines the slice's deterministic Tier 1 DM query surface.
// Inputs: read-only world state plus an optional focus question.
// Outputs: typed world and NPC-memory views that narration shells can format without inventing mechanics.
// Bible reference: ARCHITECTURE.md Part 3 DM API Tier 1 + NPC Memory store.
namespace EmberCrpg.Domain.DM
{
    /// <summary>Read-only DM query surface for grounded narration.</summary>
    public interface IDmQueryService
    {
        DmWorldStateView GetWorldState(SliceWorldState world);
        DmNpcMemoryView GetNpcMemory(SliceWorldState world, ActorId npcId);
        DmNpcMemoryView GetRelevantNpcMemory(SliceWorldState world, string question);
    }

    /// <summary>Typed current-world facts for narration shells.</summary>
    public sealed class DmWorldStateView
    {
        public DmWorldStateView(int roomSeed, bool enemyAlive, bool doorOpen, bool guardDoorAccessGranted, int inventorySlotsUsed, int inventoryCapacity, string recommendedObjective)
        {
            RoomSeed = roomSeed;
            EnemyAlive = enemyAlive;
            DoorOpen = doorOpen;
            GuardDoorAccessGranted = guardDoorAccessGranted;
            InventorySlotsUsed = inventorySlotsUsed;
            InventoryCapacity = inventoryCapacity;
            RecommendedObjective = recommendedObjective;
        }

        public int RoomSeed { get; }
        public bool EnemyAlive { get; }
        public bool DoorOpen { get; }
        public bool GuardDoorAccessGranted { get; }
        public int InventorySlotsUsed { get; }
        public int InventoryCapacity { get; }
        public string RecommendedObjective { get; }
    }

    /// <summary>Typed remembered facts for one NPC.</summary>
    public sealed class DmNpcMemoryView
    {
        public DmNpcMemoryView(ActorId npcId, string npcName, string[] recentEvents, string[] knownTopics)
        {
            NpcId = npcId;
            NpcName = npcName ?? string.Empty;
            RecentEvents = recentEvents ?? new string[0];
            KnownTopics = knownTopics ?? new string[0];
        }

        public ActorId NpcId { get; }
        public string NpcName { get; }
        public string[] RecentEvents { get; }
        public string[] KnownTopics { get; }
        public bool HasMemory => RecentEvents.Length > 0 || KnownTopics.Length > 0;
    }
}
