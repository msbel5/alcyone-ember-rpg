using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Rng;

// Design note:
// SpellExecutionService is the deterministic Sprint 5 orchestration seam for one full spell request.
// Inputs: caster, selected spell id, known-spell set, optional requested target actor, and optional
// cooldown state.
// Outputs: atomic execution where mana is committed only after cast prechecks, target routing, and
// effect-resolution support all succeed; cooldown starts only after a successful committed cast; no
// Unity dependency.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 and MASTER_MECHANICS_BIBLE.md §14-§15.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic orchestration service for one end-to-end spell execution.</summary>
    public sealed class SpellExecutionService
    {
        private readonly SpellCastingService _castingService;
        private readonly SpellTargetValidator _targetValidator;
        private readonly SpellEffectResolutionService _effectResolutionService;
        private readonly SpellCastRollService _castRollService;

        public SpellExecutionService()
            : this(new SpellCastingService(), new SpellTargetValidator(), new SpellEffectResolutionService(), new SpellCastRollService())
        {
        }

        public SpellExecutionService(
            SpellCastingService castingService,
            SpellTargetValidator targetValidator,
            SpellEffectResolutionService effectResolutionService,
            SpellCastRollService castRollService)
        {
            _castingService = castingService ?? throw new ArgumentNullException(nameof(castingService));
            _targetValidator = targetValidator ?? throw new ArgumentNullException(nameof(targetValidator));
            _effectResolutionService = effectResolutionService ?? throw new ArgumentNullException(nameof(effectResolutionService));
            _castRollService = castRollService ?? throw new ArgumentNullException(nameof(castRollService));
        }

        public SpellExecutionResult TryExecute(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            ActorRecord requestedTarget)
        {
            return TryExecute(caster, spellTemplateId, knownSpellIds, requestedTarget, null);
        }

        public SpellExecutionResult TryExecute(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            ActorRecord requestedTarget,
            SpellCooldownState cooldownState)
        {
            return TryExecuteInternal(caster, spellTemplateId, knownSpellIds, requestedTarget, cooldownState, null, useRoll: false);
        }

        public SpellExecutionResult TryExecuteWithRoll(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            ActorRecord requestedTarget,
            IDeterministicRng rng)
        {
            return TryExecuteWithRoll(caster, spellTemplateId, knownSpellIds, requestedTarget, rng, null);
        }

        public SpellExecutionResult TryExecuteWithRoll(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            ActorRecord requestedTarget,
            IDeterministicRng rng,
            SpellCooldownState cooldownState)
        {
            return TryExecuteInternal(caster, spellTemplateId, knownSpellIds, requestedTarget, cooldownState, rng, useRoll: true);
        }

        private SpellExecutionResult TryExecuteInternal(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            ActorRecord requestedTarget,
            SpellCooldownState cooldownState,
            IDeterministicRng rng,
            bool useRoll)
        {
            var preparedCast = _castingService.TryPrepareCast(caster, spellTemplateId, knownSpellIds, cooldownState);
            if (!preparedCast.Success)
                return SpellExecutionResult.Fail(
                    SpellExecutionError.CastRejected,
                    preparedCast.Spell,
                    null,
                    preparedCast,
                    null,
                    null,
                    null,
                    preparedCast.Message);

            var targetValidation = _targetValidator.Validate(preparedCast.Spell, caster, requestedTarget);
            if (!targetValidation.Success)
                return SpellExecutionResult.Fail(
                    SpellExecutionError.TargetRejected,
                    preparedCast.Spell,
                    null,
                    preparedCast,
                    null,
                    targetValidation,
                    null,
                    targetValidation.Message);

            var effectPreview = _effectResolutionService.CanResolveInstantaneousEffects(preparedCast.Spell, targetValidation.RoutedTarget);
            if (!effectPreview.Success)
                return SpellExecutionResult.Fail(
                    SpellExecutionError.ResolutionRejected,
                    preparedCast.Spell,
                    targetValidation.RoutedTarget,
                    preparedCast,
                    null,
                    targetValidation,
                    effectPreview,
                    effectPreview.Message);

            SpellCastRollResult castRollResult = null;
            if (useRoll)
            {
                castRollResult = _castRollService.Roll(caster, preparedCast.Spell, rng);
                if (castRollResult.Error != SpellCastRollError.None)
                    return SpellExecutionResult.Fail(
                        SpellExecutionError.CastRejected,
                        preparedCast.Spell,
                        targetValidation.RoutedTarget,
                        preparedCast,
                        castRollResult,
                        targetValidation,
                        null,
                        castRollResult.Message);

                if (!castRollResult.Success)
                    return SpellExecutionResult.Fail(
                        SpellExecutionError.CastFizzled,
                        preparedCast.Spell,
                        targetValidation.RoutedTarget,
                        preparedCast,
                        castRollResult,
                        targetValidation,
                        null,
                        castRollResult.Message);
            }

            var committedCast = _castingService.CommitPreparedCast(caster, preparedCast.Spell, cooldownState);
            if (!committedCast.Success)
                return SpellExecutionResult.Fail(
                    SpellExecutionError.CastRejected,
                    preparedCast.Spell,
                    targetValidation.RoutedTarget,
                    committedCast,
                    castRollResult,
                    targetValidation,
                    null,
                    committedCast.Message);

            var effectResolution = _effectResolutionService.ResolveInstantaneousEffects(committedCast, targetValidation.RoutedTarget);
            if (!effectResolution.Success)
                return SpellExecutionResult.Fail(
                    SpellExecutionError.ResolutionRejected,
                    preparedCast.Spell,
                    targetValidation.RoutedTarget,
                    committedCast,
                    castRollResult,
                    targetValidation,
                    effectResolution,
                    effectResolution.Message);

            var message = useRoll
                ? $"{caster.Name} executes {preparedCast.Spell.DisplayName} after a successful cast roll ({castRollResult.Roll}<={castRollResult.Threshold})."
                : $"{caster.Name} executes {preparedCast.Spell.DisplayName} successfully.";

            return SpellExecutionResult.Ok(
                committedCast,
                castRollResult,
                targetValidation,
                effectResolution,
                message);
        }
    }
}
