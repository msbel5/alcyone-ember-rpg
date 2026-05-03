// Design note:
// SpellTargetKind names the deterministic targeting contract for Sprint 5 spell definitions.
// Inputs: spell catalog/domain definitions choosing who or where a spell may affect.
// Outputs: stable enum for pure Domain/Simulation cost estimation and future targeting validation.
// Bible reference: MASTER_MECHANICS_BIBLE.md §14 targetMultiplier, EMBER_VISION_BIBLE.md §3 Unity-free Domain boundary.
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Deterministic targeting shape for a spell definition.</summary>
    public enum SpellTargetKind
    {
        None = 0,
        CasterSelf = 1,
        Touch = 2,
        SingleTarget = 3,
        AreaAroundCaster = 4,
        AreaAtRange = 5,
    }
}
