// Design note:
// RoomLayoutId names the slice's deterministic room templates without importing presentation concerns.
// Inputs: seed-based layout selection in simulation.
// Outputs: stable identifiers for tests, saves, and DM queries.
// Bible reference: Sprint 3 richer room templates.
namespace EmberCrpg.Domain.World
{
    /// <summary>Deterministic room templates available to the vertical slice.</summary>
    public enum RoomLayoutId
    {
        CheckpointAxis = 1,
        OffsetWatch = 2,
        SplitHall = 3,
    }
}
