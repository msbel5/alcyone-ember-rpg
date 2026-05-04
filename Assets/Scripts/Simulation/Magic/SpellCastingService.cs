using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Magic;

// Design note:
// SpellCastingService is the deterministic Sprint 5 cast validation and mana-commit gate.
// Inputs: caster ActorRecord, requested spell template id, known-spells set, and a catalog lookup.
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

        public SpellCastingService()
            : this(SliceSpellCatalog.Find)
        {
        }

        public SpellCastingService(Func<string, SpellDefinition> catalogLookup)
        {
            if (catalogLookup == null)
                throw new ArgumentNullException(nameof(catalogLookup));
            _catalogLookup = catalogLookup;
        }

        public SpellCastResult TryPrepareCast(ActorRecord caster, string spellTemplateId, IReadOnlyCollection<string> knownSpellIds)
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

            var currentMana = caster.Vitals.Mana.Current;
            if (currentMana < spell.ManaCost)
                return SpellCastResult.Fail(SpellCastError.InsufficientMana, spell, $"{caster.Name} lacks mana for {spell.DisplayName} ({currentMana}<{spell.ManaCost}).");

            return SpellCastResult.Ok(spell, 0, $"{caster.Name} is ready to cast {spell.DisplayName}.");
        }

        public SpellCastResult CommitPreparedCast(ActorRecord caster, SpellDefinition spell)
        {
            if (caster == null)
                return SpellCastResult.Fail(SpellCastError.InvalidCaster, null, "No caster supplied.");
            if (!caster.IsAlive)
                return SpellCastResult.Fail(SpellCastError.InvalidCaster, spell, $"{caster.Name} cannot cast while incapacitated.");
            if (spell == null)
                return SpellCastResult.Fail(SpellCastError.SpellNotFound, null, "No prepared spell supplied.");

            var currentMana = caster.Vitals.Mana.Current;
            if (currentMana < spell.ManaCost)
                return SpellCastResult.Fail(SpellCastError.InsufficientMana, spell, $"{caster.Name} lacks mana for {spell.DisplayName} ({currentMana}<{spell.ManaCost}).");

            caster.ApplyVitals(caster.Vitals.WithMana(caster.Vitals.Mana.Damage(spell.ManaCost)));
            return SpellCastResult.Ok(spell, spell.ManaCost, $"{caster.Name} casts {spell.DisplayName} (-{spell.ManaCost} mana).");
        }

        public SpellCastResult TryCast(ActorRecord caster, string spellTemplateId, IReadOnlyCollection<string> knownSpellIds)
        {
            var prepared = TryPrepareCast(caster, spellTemplateId, knownSpellIds);
            if (!prepared.Success)
                return prepared;

            return CommitPreparedCast(caster, prepared.Spell);
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
