// Design note:
// DungeonSpawnKind identifies deterministic room-local placement anchors in generated dungeons.
// Inputs: generator-assigned archetype placement.
// Outputs: stable spawn categories for actors, pickups, tests, and save mapping.
// Bible reference: MASTER_MECHANICS_BIBLE.md deterministic world lock-in, Sprint 4 Phase 3 scope.
namespace EmberCrpg.Domain.World
{
    /// <summary>Stable archetype labels for generated dungeon spawn points.</summary>
    public enum DungeonSpawnKind
    {
        Player = 0,
        Talker = 1,
        Merchant = 2,
        Guard = 3,
        Enemy = 4,
        Pickup = 5,
    }
}
