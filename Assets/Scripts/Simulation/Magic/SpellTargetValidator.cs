using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellTargetValidator is the deterministic Sprint 5 target/route gate that sits between the cast
// validation (mana spend) and the effect resolver. It refuses target shapes the resolver cannot honour
// yet (area kinds) and routes the supported shapes (CasterSelf, Touch, SingleTarget) to a concrete
// ActorRecord so the resolver does not have to second-guess its input.
// Inputs: SpellDefinition, caster ActorRecord, optional requested ActorRecord target.
// Outputs: SpellTargetValidationResult with the routed target on success, deterministic refusal on failure.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 (Unity-free Domain/Simulation),
// MASTER_MECHANICS_BIBLE.md §14 targetMultiplier (target taxonomy),
// EMBER_VISION_BIBLE.md §8 Sprint 5 (deterministic mechanics, area systems land later).
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic validator that routes a spell to its concrete target actor.</summary>
    public sealed class SpellTargetValidator
    {
        public SpellTargetValidationResult Validate(SpellDefinition spell, ActorRecord caster, ActorRecord requestedTarget)
        {
            if (spell == null)
                return SpellTargetValidationResult.Fail(SpellTargetValidationError.InvalidSpell, null, "No spell supplied for target validation.");
            if (caster == null)
                return SpellTargetValidationResult.Fail(SpellTargetValidationError.InvalidCaster, spell, "No caster supplied for target validation.");
            if (!caster.IsAlive)
                return SpellTargetValidationResult.Fail(SpellTargetValidationError.InvalidCaster, spell, $"{caster.Name} cannot target while incapacitated.");

            switch (spell.TargetKind)
            {
                case SpellTargetKind.CasterSelf:
                    return ValidateCasterSelf(spell, caster, requestedTarget);
                case SpellTargetKind.Touch:
                    return ValidateTouch(spell, caster, requestedTarget);
                case SpellTargetKind.SingleTarget:
                    return ValidateSingleTarget(spell, requestedTarget);
                case SpellTargetKind.AreaAroundCaster:
                case SpellTargetKind.AreaAtRange:
                    return SpellTargetValidationResult.Fail(
                        SpellTargetValidationError.UnsupportedTargetKind,
                        spell,
                        $"{spell.DisplayName} uses {spell.TargetKind} targeting which is not supported until area resolution lands.");
                default:
                    return SpellTargetValidationResult.Fail(
                        SpellTargetValidationError.UnsupportedTargetKind,
                        spell,
                        $"{spell.DisplayName} declares an unsupported target kind ({spell.TargetKind}).");
            }
        }

        private static SpellTargetValidationResult ValidateCasterSelf(SpellDefinition spell, ActorRecord caster, ActorRecord requestedTarget)
        {
            if (requestedTarget != null && !caster.Id.Equals(requestedTarget.Id))
                return SpellTargetValidationResult.Fail(
                    SpellTargetValidationError.WrongTargetForSelfSpell,
                    spell,
                    $"{spell.DisplayName} only affects {caster.Name}.");

            return SpellTargetValidationResult.Ok(spell, caster, $"{spell.DisplayName} routed to caster {caster.Name}.");
        }

        private static SpellTargetValidationResult ValidateTouch(SpellDefinition spell, ActorRecord caster, ActorRecord requestedTarget)
        {
            if (requestedTarget == null)
                return SpellTargetValidationResult.Fail(
                    SpellTargetValidationError.InvalidTarget,
                    spell,
                    $"{spell.DisplayName} requires a touch target.");
            if (!requestedTarget.IsAlive)
                return SpellTargetValidationResult.Fail(
                    SpellTargetValidationError.InvalidTarget,
                    spell,
                    $"{spell.DisplayName} cannot touch {requestedTarget.Name} (incapacitated).");

            var distance = caster.Position.ManhattanDistanceTo(requestedTarget.Position);
            if (distance != 1)
                return SpellTargetValidationResult.Fail(
                    SpellTargetValidationError.TargetNotAdjacent,
                    spell,
                    $"{spell.DisplayName} requires an orthogonally adjacent target (distance={distance}).");

            return SpellTargetValidationResult.Ok(spell, requestedTarget, $"{spell.DisplayName} routed to touched {requestedTarget.Name}.");
        }

        private static SpellTargetValidationResult ValidateSingleTarget(SpellDefinition spell, ActorRecord requestedTarget)
        {
            if (requestedTarget == null)
                return SpellTargetValidationResult.Fail(
                    SpellTargetValidationError.InvalidTarget,
                    spell,
                    $"{spell.DisplayName} requires a single target.");
            if (!requestedTarget.IsAlive)
                return SpellTargetValidationResult.Fail(
                    SpellTargetValidationError.InvalidTarget,
                    spell,
                    $"{spell.DisplayName} cannot target {requestedTarget.Name} (incapacitated).");

            return SpellTargetValidationResult.Ok(spell, requestedTarget, $"{spell.DisplayName} routed to {requestedTarget.Name}.");
        }
    }
}
