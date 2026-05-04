// Design note:
// SpellExecutionError gives deterministic failure-stage categories for the Sprint 5 orchestration service.
// Inputs: cast, target-validation, or effect-resolution refusals.
// Outputs: stable non-localized stage ids for tests and future UI.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 and §8 Sprint 5 deterministic layering.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Stable outcome code for a spell execution attempt.</summary>
    public enum SpellExecutionError
    {
        None = 0,
        CastRejected = 1,
        TargetRejected = 2,
        ResolutionRejected = 3,
    }
}
