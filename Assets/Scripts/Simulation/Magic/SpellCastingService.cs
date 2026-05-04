using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellCastingService is the deterministic Sprint 5 cast validation and mana-commit gate.
// Inputs: caster ActorRecord, requested spell template id, known-spells set, optional cooldown state,
// and a catalog lookup.
// Outputs: preflight validation without mutation plus an explicit mana-spend commit step.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3 + §8 Sprint 5 (deterministic foundation, no AI),
// MASTER_MECHANICS_BIBLE.md §14 (cost <= mana, school taxonomy),
// EMBER_VISION_BIBLE.md §11 reference rule (read references/openmw-master/apps/openmw/mwmechanics/spells.cpp for shape only).
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic cast validation and mana-spend service.</summary>
    public sealed class SpellCastingService
    {
        private readonly Func<string, SpellDefinition> _catalogLookup;
        private readonly SpellCooldownService _cooldownService;

        public SpellCastingService()
            : this(SliceSpellCatalog.Find, new SpellCooldownService())
        {
        }

        public SpellCastingService(Func<string, SpellDefinition> catalogLookup)
            : this(catalogLookup, new SpellCooldownService())
        {
        }

        public SpellCastingService(Func<string, SpellDefinition> catalogLookup, SpellCooldownService cooldownService)
        {
            if (catalogLookup == null)
                throw new ArgumentNullException(nameof(catalogLookup));
            if (cooldownService == null)
                throw new ArgumentNullException(nameof(cooldownService));

            _catalogLookup = catalogLookup;
            _cooldownService = cooldownService;
        }

        public SpellCastResult TryPrepareCast(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            SpellCooldownState cooldownState = null)
        {
            if (caster == null)
                return SpellCastResult.Fail(SpellCastError.InvalidCaster, null, "No caster supplied.");
            if (!caster.IsAlive)
                return SpellCastResult.Fail(SpellCastError.InvalidCaster, null, $"{caster.Name} cannot cast while incapacitated.");
            if (string.IsNullOrWhiteSpace(spellTemplateId))
                return SpellCastResult.Fail(SpellCastError.SpellNotFound, null, "No spell selected.");

            var spell = _catalogLookup(spellTemplateId);
            if (spell == null)
                return SpellCastResult.Fail(SpellCastError.SpellNotFound, null, $"Unknown spell '{spellTemplateId}'.");

            if (knownSpellIds == null || !ContainsKnown(knownSpellIds, spell.TemplateId))
                return SpellCastResult.Fail(SpellCastError.SpellNotKnown, spell, $"{caster.Name} has not learned {spell.DisplayName}.");

            var remainingCooldownTicks = _cooldownService.GetRemainingTicks(spell, cooldownState);
            if (remainingCooldownTicks > 0)
                return SpellCastResult.Fail(SpellCastError.SpellOnCooldown, spell, $"{spell.DisplayName} is on cooldown for {remainingCooldownTicks} more ticks.");

            var currentMana = caster.Vitals.Mana.Current;
            if (currentMana < spell.ManaCost)
                return SpellCastResult.Fail(SpellCastError.InsufficientMana, spell, $"{caster.Name} lacks mana for {spell.DisplayName} ({currentMana}<{spell.ManaCost}).");

            return SpellCastResult.Ok(spell, 0, $"{caster.Name} is ready to cast {spell.DisplayName}.");
        }

        public SpellCastResult CommitPreparedCast(ActorRecord caster, SpellDefinition spell, SpellCooldownState cooldownState = null)
        {
            if (caster == null)
                return SpellCastResult.Fail(SpellCastError.InvalidCaster, null, "No caster supplied.");
            if (!caster.IsAlive)
                return SpellCastResult.Fail(SpellCastError.InvalidCaster, spell, $"{caster.Name} cannot cast while incapacitated.");
            if (spell == null)
                return SpellCastResult.Fail(SpellCastError.SpellNotFound, null, "No prepared spell supplied.");

            var remainingCooldownTicks = _cooldownService.GetRemainingTicks(spell, cooldownState);
            if (remainingCooldownTicks > 0)
                return SpellCastResult.Fail(SpellCastError.SpellOnCooldown, spell, $"{spell.DisplayName} is on cooldown for {remainingCooldownTicks} more ticks.");

            var currentMana = caster.Vitals.Mana.Current;
            if (currentMana < spell.ManaCost)
                return SpellCastResult.Fail(SpellCastError.InsufficientMana, spell, $"{caster.Name} lacks mana for {spell.DisplayName} ({currentMana}<{spell.ManaCost}).");

            caster.ApplyVitals(caster.Vitals.WithMana(caster.Vitals.Mana.Damage(spell.ManaCost)));
            _cooldownService.StartCooldown(spell, cooldownState);

            var message = spell.CooldownTicks > 0
                ? $"{caster.Name} casts {spell.DisplayName} (-{spell.ManaCost} mana, cooldown {spell.CooldownTicks} ticks)."
                : $"{caster.Name} casts {spell.DisplayName} (-{spell.ManaCost} mana).";

            return SpellCastResult.Ok(spell, spell.ManaCost, message);
        }

        public SpellCastResult TryCast(
            ActorRecord caster,
            string spellTemplateId,
            IReadOnlyCollection<string> knownSpellIds,
            SpellCooldownState cooldownState = null)
        {
            var prepared = TryPrepareCast(caster, spellTemplateId, knownSpellIds, cooldownState);
            if (!prepared.Success)
                return prepared;

            return CommitPreparedCast(caster, prepared.Spell, cooldownState);
        }

        private static bool ContainsKnown(IReadOnlyCollection<string> knownSpellIds, string templateId)
        {
            foreach (var known in knownSpellIds)
            {
                if (string.Equals(known, templateId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
