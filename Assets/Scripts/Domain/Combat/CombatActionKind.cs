// Design note:
// CombatActionKind lists the first RTWP combat verbs without tying them to input devices or animation assets.
// Inputs: player/enemy action requests from simulation or presentation.
// Outputs: deterministic queue entries for melee, block, dodge, and cast timing.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Faz 2 real-time foundation.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Supported real-time-with-pause combat actions for the Sprint 4 foundation.</summary>
    public enum CombatActionKind
    {
        MeleeSwing = 0,
        Block = 1,
        Dodge = 2,
        Cast = 3,
    }
}
