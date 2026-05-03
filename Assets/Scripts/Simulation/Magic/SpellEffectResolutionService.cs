using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellEffectResolutionService applies the deterministic Sprint 5 instantaneous magic subset.
// Inputs: a successful SpellCastResult and one target ActorRecord.
// Outputs: target health mutations for DirectDamage/RestoreHealth only; no mana changes or Unity types.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 + §8 Sprint 5 deterministic mechanics,
// MASTER_MECHANICS_BIBLE.md §15 Magic effects/opcodes.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic resolver for immediate spell effects.</summary>
    public sealed class SpellEffectResolutionService
    {
        public SpellEffectResolutionResult ResolveInstantaneousEffects(SpellCastResult castResult, ActorRecord target)
        {
            if (castResult == null || !castResult.Success || castResult.Spell == null)
                return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.InvalidCast, null, "A successful cast is required before resolving effects.");
            if (target == null || !target.IsAlive)
                return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.InvalidTarget, castResult.Spell, "A living target is required for spell effect resolution.");

            var effects = castResult.Spell.Effects;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (!effect.IsInstantaneous)
                    return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.NonInstantaneousEffect, castResult.Spell, "Timed spell effects are not resolved by this service.");
                if (!IsSupported(effect.Kind))
                    return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.UnsupportedEffect, castResult.Spell, "Only direct damage and restore health are supported in this increment.");
            }

            var totalDamage = 0;
            var totalHealing = 0;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                var before = target.Vitals.Health.Current;
                if (effect.Kind == SpellEffectKind.DirectDamage)
                {
                    target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Damage(effect.Magnitude)));
                    totalDamage += before - target.Vitals.Health.Current;
                }
                else if (effect.Kind == SpellEffectKind.RestoreHealth)
                {
                    target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Restore(effect.Magnitude)));
                    totalHealing += target.Vitals.Health.Current - before;
                }
            }

            return SpellEffectResolutionResult.Ok(
                castResult.Spell,
                effects.Count,
                totalDamage,
                totalHealing,
                $"Resolved {effects.Count} instantaneous effect(s) from {castResult.Spell.DisplayName}.");
        }

        private static bool IsSupported(SpellEffectKind kind)
        {
            return kind == SpellEffectKind.DirectDamage || kind == SpellEffectKind.RestoreHealth;
        }
    }
}
