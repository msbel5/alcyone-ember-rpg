// Design note:
// SpellEffectKind enumerates the Sprint 5 starter effect verbs for the magic foundation.
// Inputs: spell definitions composing one or more effect specs.
// Outputs: stable enum used by future effect resolvers; this sprint only validates and pins shape.
// Bible reference: MASTER_MECHANICS_BIBLE.md §15 Magic — Effects & Opcodes (Destruction/Restoration subset),
// EMBER_VISION_BIBLE.md §11 reference rule (read references/openmw-master/apps/openmw/mwmechanics/spells.cpp for shape only).
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Starter set of deterministic spell effect verbs for the foundation slice.</summary>
    public enum SpellEffectKind
    {
        None = 0,
        DirectDamage = 1,
        RestoreHealth = 2,
        RestoreFatigue = 3,
        ShieldBuff = 4,
    }
}
