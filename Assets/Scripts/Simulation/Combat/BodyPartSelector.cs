using EmberCrpg.Domain.Combat;
using EmberCrpg.Simulation.Rng;

// Design note:
// BodyPartSelector ports DFU's weighted seven-part targeting array for Sprint 1 combat.
// Inputs: deterministic RNG.
// Outputs: one weighted body-part selection per resolved strike.
// Bible reference: MASTER_MECHANICS_BIBLE.md §10 body-part weights, PRD FR-02.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Deterministic weighted hit-location selector.</summary>
    public sealed class BodyPartSelector
    {
        private static readonly BodyPart[] WeightedParts =
        {
            BodyPart.Head,
            BodyPart.Head,
            BodyPart.RightArm,
            BodyPart.RightArm,
            BodyPart.RightArm,
            BodyPart.LeftArm,
            BodyPart.LeftArm,
            BodyPart.LeftArm,
            BodyPart.Chest,
            BodyPart.Chest,
            BodyPart.Chest,
            BodyPart.Chest,
            BodyPart.Hands,
            BodyPart.Hands,
            BodyPart.Hands,
            BodyPart.Hands,
            BodyPart.Legs,
            BodyPart.Legs,
            BodyPart.Legs,
            BodyPart.Feet,
        };

        public BodyPart Select(IDeterministicRng rng)
        {
            return WeightedParts[rng.NextInt(WeightedParts.Length)];
        }
    }
}
