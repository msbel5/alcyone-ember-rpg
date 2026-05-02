// Design note:
// MagicSchool names the six deterministic spell schools used by Sprint 5's magic foundation.
// Inputs: spell definitions tagging their school for catalog filtering and future skill mapping.
// Outputs: stable enum for Domain/Simulation/UI without coupling to opcode tables.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, §8 Sprint Mapping, §11 reference rule.
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Deterministic school tag for a spell definition.</summary>
    public enum MagicSchool
    {
        None = 0,
        Destruction = 1,
        Restoration = 2,
        Illusion = 3,
        Conjuration = 4,
        Mysticism = 5,
        Alteration = 6,
    }
}
