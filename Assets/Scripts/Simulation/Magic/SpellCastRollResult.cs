using EmberCrpg.Domain.Magic;

// Design note:
// SpellCastRollResult is the Tier 3 shape for one deterministic spell-cast roll: roll value,
// threshold, and the full Tier 2 chance breakdown that produced the threshold.
// Inputs: SpellCastRollService, which combines SpellSuccessChanceService (Tier 2) with a seeded
// IDeterministicRng percentage roll (Tier 3).
// Outputs: stable success/error code, the d100 that came up, the threshold it had to beat or match,
// the upstream chance breakdown, and a human-readable line for narration or DM transcripts.
// Bible reference: docs/mechanics/ARCHITECTURE.md §3.3 RollResult shape (success, rollValue,
// threshold, breakdown).
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Result of one deterministic spell-cast roll attempt.</summary>
    public sealed class SpellCastRollResult
    {
        private SpellCastRollResult(
            bool success,
            SpellCastRollError error,
            SpellDefinition spell,
            int roll,
            int threshold,
            SpellSuccessChanceResult chance,
            string message)
        {
            Success = success;
            Error = error;
            Spell = spell;
            Roll = roll;
            Threshold = threshold;
            Chance = chance;
            Message = message;
        }

        public bool Success { get; }
        public SpellCastRollError Error { get; }
        public SpellDefinition Spell { get; }
        /// <summary>The d100 percentage roll (1..100). Zero when the roll was never produced.</summary>
        public int Roll { get; }
        /// <summary>The success-chance percentage the roll had to land on or below.</summary>
        public int Threshold { get; }
        /// <summary>The full upstream chance breakdown that produced <see cref="Threshold"/>.</summary>
        public SpellSuccessChanceResult Chance { get; }
        public string Message { get; }

        public static SpellCastRollResult Ok(
            SpellDefinition spell,
            int roll,
            int threshold,
            SpellSuccessChanceResult chance,
            string message)
        {
            return new SpellCastRollResult(true, SpellCastRollError.None, spell, roll, threshold, chance, message);
        }

        public static SpellCastRollResult Miss(
            SpellDefinition spell,
            int roll,
            int threshold,
            SpellSuccessChanceResult chance,
            string message)
        {
            return new SpellCastRollResult(false, SpellCastRollError.None, spell, roll, threshold, chance, message);
        }

        public static SpellCastRollResult Fail(
            SpellCastRollError error,
            SpellDefinition spell,
            SpellSuccessChanceResult chance,
            string message)
        {
            return new SpellCastRollResult(false, error, spell, 0, 0, chance, message);
        }
    }
}
