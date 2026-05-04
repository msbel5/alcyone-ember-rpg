using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;

// Design note:
// SpellExecutionService is the deterministic Sprint 5 orchestration seam for one full spell request.
// Inputs: caster, selected spell id, known-spell set, and optional requested target actor.
// Outputs: atomic execution where mana is committed only after cast prechecks, target routing, and
// effect-resolution support all succeed; no Unity dependency.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 and MASTER_MECHANICS_BIBLE.md §14-§15.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic orchestration service for one end-to-end spell execution.</summary>
    public sealed class SpellExecutionService
    {
        private readonly SpellCastingService _castingService;
        private readonly SpellTargetValidator _targetValidator;
        private readonly SpellEffectResolutionService _effectResolutionService;

        public SpellExecutionService()
            : this(new SpellCastingService(), new SpellTargetValidator(), new SpellEffectResolutionService())
        {
        }

        public SpellExecutionService(
            SpellCastingService castingService,
            SpellTargetValidator targetValidator,
            SpellEffectResolutionService effectResolutionService)
        {
            _castingService = castingService ?? throw new ArgumentNullException(nameof(castingService));
            _targetValidator = targetValidator ?? throw new ArgumentNullException(nameof(targetValidator));
            _effectResolutionService = effectResolutionService ?? throw new ArgumentNullException(nameof(effectResolutionService));
        }

        public SpellExecutionResult TryExecute(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            ActorRecord requestedTarget)
        {
            var preparedCast = _castingService.TryPrepareCast(caster, spellTemplateId, knownSpellIds);
            if (!preparedCast.Success)
                return SpellExecutionResult.Fail(
                    SpellExecutionError.CastRejected,
                    preparedCast.Spell,
                    null,
                    preparedCast,
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
                    targetValidation,
                    effectPreview,
                    effectPreview.Message);

            var committedCast = _castingService.CommitPreparedCast(caster, preparedCast.Spell);
            if (!committedCast.Success)
                return SpellExecutionResult.Fail(
                    SpellExecutionError.CastRejected,
                    preparedCast.Spell,
                    targetValidation.RoutedTarget,
                    committedCast,
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
                    targetValidation,
                    effectResolution,
                    effectResolution.Message);

            return SpellExecutionResult.Ok(
                committedCast,
                targetValidation,
                effectResolution,
                $"{caster.Name} executes {preparedCast.Spell.DisplayName} successfully.");
        }
    }
}
