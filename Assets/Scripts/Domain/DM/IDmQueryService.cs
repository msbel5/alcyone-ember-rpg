using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// IDmQueryService defines the slice's deterministic DM query surface.
// Inputs: read-only world state plus an optional focus question.
// Outputs: typed world, inspection, and NPC-memory views that narration shells can format without inventing mechanics.
// Bible reference: ARCHITECTURE.md Part 3 DM API Tier 1 + NPC Memory store.
namespace EmberCrpg.Domain.DM
{
    /// <summary>Read-only DM query surface for grounded narration.</summary>
    public interface IDmQueryService
    {
        DmWorldStateView GetWorldState(SliceWorldState world);
        DmNpcMemoryView GetNpcMemory(SliceWorldState world, ActorId npcId);
        DmNpcMemoryView GetRelevantNpcMemory(SliceWorldState world, string question);
        DmInspectionView GetInspection(SliceWorldState world, string question);
    }

    /// <summary>Query tiers supported by the slice narrator shell.</summary>
    public enum DmQueryTier
    {
        Summary = 1,
        Detail = 2,
        Narrative = 3,
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

    /// <summary>Typed deeper inspection facts for detail/narrative tiers.</summary>
    public sealed class DmInspectionView
    {
        public DmInspectionView(string roomLayout, string guardAttitude, int watchReputation, string equippedWeapon, string equippedArmor, int remainingPickups, string focusReason)
        {
            RoomLayout = roomLayout ?? string.Empty;
            GuardAttitude = guardAttitude ?? string.Empty;
            WatchReputation = watchReputation;
            EquippedWeapon = equippedWeapon ?? "none";
            EquippedArmor = equippedArmor ?? "none";
            RemainingPickups = remainingPickups;
            FocusReason = focusReason ?? string.Empty;
        }

        public string RoomLayout { get; }
        public string GuardAttitude { get; }
        public int WatchReputation { get; }
        public string EquippedWeapon { get; }
        public string EquippedArmor { get; }
        public int RemainingPickups { get; }
        public string FocusReason { get; }
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
