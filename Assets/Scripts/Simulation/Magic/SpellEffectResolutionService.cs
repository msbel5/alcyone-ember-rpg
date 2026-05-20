using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellEffectResolutionService applies the deterministic Sprint 5 instantaneous magic subset
// and writes timed ShieldBuff effects from a successful cast into a ShieldBuffState container.
// Inputs: a successful SpellCastResult, one optional target ActorRecord (instantaneous path),
// and one optional ShieldBuffState (timed-buff path).
// Outputs: target vitality mutations for DirectDamage/RestoreHealth/RestoreFatigue/RestoreMana/
// DirectMana/DirectFatigue (instantaneous) and ShieldBuffState entries keyed by spell.TemplateId
// (timed buffs); the caster's mana cost is paid by SpellCastingService before resolution —
// RestoreMana here only restores the target's mana pool deterministically and does not refund the
// cast cost, DirectMana drains the target's mana pool deterministically with no caster-side
// feedback, and DirectFatigue drains the target's fatigue pool deterministically with no
// caster-side feedback. No tick-down, no actor-keyed wiring, no Unity types.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 + §8 Sprint 5 deterministic mechanics,
// MASTER_MECHANICS_BIBLE.md §15 Magic effects/opcodes.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic resolver for immediate spell effects.</summary>
    public sealed class SpellEffectResolutionService
    {
        public SpellEffectResolutionResult CanResolveInstantaneousEffects(SpellDefinition spell, ActorRecord target)
        {
            var validation = ValidateInstantaneousEffects(spell, target);
            if (!validation.Success)
                return validation;

            return SpellEffectResolutionResult.Ok(
                spell,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                $"{spell.DisplayName} is ready for instantaneous resolution against {target.Name}.");
        }

        public SpellEffectResolutionResult ResolveInstantaneousEffects(SpellCastResult castResult, ActorRecord target)
        {
            if (castResult == null || !castResult.Success || castResult.Spell == null)
                return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.InvalidCast, null, "A successful cast is required before resolving effects.");

            var validation = ValidateInstantaneousEffects(castResult.Spell, target);
            if (!validation.Success)
                return validation;

            var effects = castResult.Spell.Effects;
            var totalDamage = 0;
            var totalHealing = 0;
            var totalRestoredFatigue = 0;
            var totalRestoredMana = 0;
            var totalDirectManaDamage = 0;
            var totalDirectFatigueDamage = 0;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                var before = target.Vitals.Health.Current;
                if (effect.Kind == SpellEffectCode.DirectDamage)
                {
                    target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Damage(effect.Magnitude)));
                    totalDamage += before - target.Vitals.Health.Current;
                }
                else if (effect.Kind == SpellEffectCode.RestoreHealth)
                {
                    target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Restore(effect.Magnitude)));
                    totalHealing += target.Vitals.Health.Current - before;
                }
                else if (effect.Kind == SpellEffectCode.RestoreFatigue)
                {
                    before = target.Vitals.Fatigue.Current;
                    target.ApplyVitals(target.Vitals.WithFatigue(target.Vitals.Fatigue.Restore(effect.Magnitude)));
                    totalRestoredFatigue += target.Vitals.Fatigue.Current - before;
                }
                else if (effect.Kind == SpellEffectCode.RestoreMana)
                {
                    before = target.Vitals.Mana.Current;
                    target.ApplyVitals(target.Vitals.WithMana(target.Vitals.Mana.Restore(effect.Magnitude)));
                    totalRestoredMana += target.Vitals.Mana.Current - before;
                }
                else if (effect.Kind == SpellEffectCode.DirectMana)
                {
                    before = target.Vitals.Mana.Current;
                    target.ApplyVitals(target.Vitals.WithMana(target.Vitals.Mana.Damage(effect.Magnitude)));
                    totalDirectManaDamage += before - target.Vitals.Mana.Current;
                }
                else if (effect.Kind == SpellEffectCode.DirectFatigue)
                {
                    before = target.Vitals.Fatigue.Current;
                    target.ApplyVitals(target.Vitals.WithFatigue(target.Vitals.Fatigue.Damage(effect.Magnitude)));
                    totalDirectFatigueDamage += before - target.Vitals.Fatigue.Current;
                }
            }

            return SpellEffectResolutionResult.Ok(
                castResult.Spell,
                effects.Count,
                totalDamage,
                totalHealing,
                totalRestoredFatigue,
                totalRestoredMana,
                totalDirectManaDamage,
                totalDirectFatigueDamage,
                $"Resolved {effects.Count} instantaneous effect(s) from {castResult.Spell.DisplayName}.");
        }

        public ShieldBuffApplicationResult ApplyShieldBuffs(SpellCastResult castResult, ShieldBuffState shieldBuffState)
        {
            if (castResult == null || !castResult.Success || castResult.Spell == null)
                return ShieldBuffApplicationResult.Fail(SpellEffectResolutionError.InvalidCast, null, "A successful cast is required before applying shield buffs.");
            if (shieldBuffState == null)
                return ShieldBuffApplicationResult.Fail(SpellEffectResolutionError.InvalidBuffState, castResult.Spell, "A shield buff state container is required to record timed buffs.");

            var spell = castResult.Spell;
            var effects = spell.Effects;
            var appliedBuffCount = 0;
            var totalAppliedMagnitude = 0;
            var totalAppliedDurationTicks = 0;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect.Kind != SpellEffectCode.ShieldBuff || effect.IsInstantaneous)
                    continue;

                shieldBuffState.SetActiveBuff(spell.TemplateId, effect.DurationTicks, effect.Magnitude);
                appliedBuffCount++;
                totalAppliedMagnitude += effect.Magnitude;
                totalAppliedDurationTicks += effect.DurationTicks;
            }

            return ShieldBuffApplicationResult.Ok(
                spell,
                appliedBuffCount,
                totalAppliedMagnitude,
                totalAppliedDurationTicks,
                appliedBuffCount == 0
                    ? $"No timed shield buff effects to apply from {spell.DisplayName}."
                    : $"Applied {appliedBuffCount} timed shield buff effect(s) from {spell.DisplayName}.");
        }

        // Actor-keyed application seam: routes a successful cast's timed shield-buff effects into the
        // per-actor bag owned by ShieldBuffStateRegistry.GetOrCreate(actorId). Pure delegation to the
        // existing single-bag overload — same rejection contracts (null/failed cast, no spell), same
        // ApplyShieldBuffs result shape, no decay/save/absorption changes. Bag creation is lazy: a
        // failed cast never materializes new actor state because the registry is only touched after
        // the cast guard passes. Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3.
        public ShieldBuffApplicationResult ApplyShieldBuffs(SpellCastResult castResult, ShieldBuffStateRegistry registry, string actorId)
        {
            if (castResult == null || !castResult.Success || castResult.Spell == null)
                return ShieldBuffApplicationResult.Fail(SpellEffectResolutionError.InvalidCast, null, "A successful cast is required before applying shield buffs.");
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (string.IsNullOrWhiteSpace(actorId))
                throw new ArgumentException("Actor id must be a non-empty stable id.", nameof(actorId));

            var shieldBuffState = registry.GetOrCreate(actorId);
            return ApplyShieldBuffs(castResult, shieldBuffState);
        }

        private static SpellEffectResolutionResult ValidateInstantaneousEffects(SpellDefinition spell, ActorRecord target)
        {
            if (spell == null)
                return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.InvalidCast, null, "A spell definition is required before resolving effects.");
            if (target == null || !target.IsAlive)
                return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.InvalidTarget, spell, "A living target is required for spell effect resolution.");

            var effects = spell.Effects;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (!effect.IsInstantaneous)
                    return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.NonInstantaneousEffect, spell, "Timed spell effects are not resolved by this service.");
                if (!IsSupported(effect.Kind))
                    return SpellEffectResolutionResult.Fail(SpellEffectResolutionError.UnsupportedEffect, spell, "Only direct damage, restore health, restore fatigue, restore mana, direct mana, and direct fatigue are supported in this increment.");
            }

            return SpellEffectResolutionResult.Ok(spell, 0, 0, 0, 0, 0, 0, 0, $"{spell.DisplayName} passed instantaneous effect validation.");
        }

        private static bool IsSupported(SpellEffectCode kind)
        {
            return kind == SpellEffectCode.DirectDamage
                || kind == SpellEffectCode.RestoreHealth
                || kind == SpellEffectCode.RestoreFatigue
                || kind == SpellEffectCode.RestoreMana
                || kind == SpellEffectCode.DirectMana
                || kind == SpellEffectCode.DirectFatigue;
        }
    }
}
