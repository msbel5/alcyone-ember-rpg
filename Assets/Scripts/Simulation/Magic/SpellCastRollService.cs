using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Rng;

// Design note:
// SpellCastRollService is the Sprint 5 deterministic Tier 3 roll seam for spell casting. It pairs
// the existing Tier 2 SpellSuccessChanceService threshold with a seeded IDeterministicRng percentage
// roll and reports the full breakdown without mutating SpellExecutionService yet.
// Inputs: caster record, spell definition, and a seeded deterministic RNG.
// Outputs: SpellCastRollResult carrying success, roll value, threshold, the upstream chance
// breakdown, and a narration line. No game state is mutated, no mana is committed.
// Bible reference: docs/mechanics/ARCHITECTURE.md §3.3 Tier 3 RollResult, §3.2 ComputeSpellSuccessChance,
// MASTER_MECHANICS_BIBLE.md §14 OpenMW casting note.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic seam that rolls one spell-cast attempt against its success threshold.</summary>
    public sealed class SpellCastRollService
    {
        private readonly SpellSuccessChanceService _chanceService;

        public SpellCastRollService()
            : this(new SpellSuccessChanceService())
        {
        }

        public SpellCastRollService(SpellSuccessChanceService chanceService)
        {
            _chanceService = chanceService ?? throw new ArgumentNullException(nameof(chanceService));
        }

        public SpellCastRollResult Roll(ActorRecord caster, SpellDefinition spell, IDeterministicRng rng)
        {
            if (caster == null)
                return SpellCastRollResult.Fail(SpellCastRollError.InvalidCaster, spell, null, "A living caster record is required to roll a spell cast.");
            if (spell == null)
                return SpellCastRollResult.Fail(SpellCastRollError.InvalidSpell, null, null, "A spell definition is required to roll a spell cast.");
            if (rng == null)
                return SpellCastRollResult.Fail(SpellCastRollError.InvalidRng, spell, null, $"A seeded deterministic RNG is required to roll {spell.DisplayName}.");

            var chance = _chanceService.Calculate(caster, spell);
            if (!chance.Success)
            {
                // PR#20 bot review fix: previously only InvalidCaster was
                // mapped, so an InvalidSpell error from the chance service
                // was collapsed into the generic ChanceCalculationFailed and
                // callers lost the specific error type for telemetry/replay.
                SpellCastRollError failError;
                if (chance.Error == SpellSuccessChanceError.InvalidCaster)
                    failError = SpellCastRollError.InvalidCaster;
                else if (chance.Error == SpellSuccessChanceError.InvalidSpell)
                    failError = SpellCastRollError.InvalidSpell;
                else
                    failError = SpellCastRollError.ChanceCalculationFailed;
                return SpellCastRollResult.Fail(failError, spell, chance, chance.Message);
            }

            var threshold = chance.ChancePercent;
            var roll = rng.RollPercent();

            if (roll <= threshold)
                return SpellCastRollResult.Ok(
                    spell,
                    roll,
                    threshold,
                    chance,
                    $"{caster.Name} lands {spell.DisplayName} ({roll}<={threshold}).");

            return SpellCastRollResult.Miss(
                spell,
                roll,
                threshold,
                chance,
                $"{caster.Name} fizzles {spell.DisplayName} ({roll}>{threshold}).");
        }
    }
}
