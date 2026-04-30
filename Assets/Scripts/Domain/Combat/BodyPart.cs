// Design note:
// BodyPart locks Sprint 1 to DFU's seven weighted target zones.
// Inputs: deterministic body-part selection during combat resolution.
// Outputs: stable hit-location enum values for logs and mitigation.
// Bible reference: MASTER_MECHANICS_BIBLE.md §10, PRD FR-02.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Seven body parts used by the slice combat kernel.</summary>
    public enum BodyPart
    {
        Head = 0,
        RightArm = 1,
        LeftArm = 2,
        Chest = 3,
        Hands = 4,
        Legs = 5,
        Feet = 6,
    }
}
