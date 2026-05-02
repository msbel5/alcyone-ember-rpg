using System;
using System.Collections.Generic;

// Design note:
// SpellDefinition is the immutable catalog entry for one spell: stable template id,
// display name, school tag, mana cost, and an ordered list of effect specs.
// Inputs: deterministic catalog data (no Unity types).
// Outputs: read-only spell record consumed by casting/validation services.
// Bible reference: MASTER_MECHANICS_BIBLE.md §14 (cost <= mana check, school taxonomy),
// EMBER_VISION_BIBLE.md §3 Layer 1/3 boundary, §11 reference rule (clean-room shape only).
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Immutable catalog entry describing one castable spell.</summary>
    public sealed class SpellDefinition
    {
        private readonly SpellEffectSpec[] _effects;

        public SpellDefinition(
            string templateId,
            string displayName,
            MagicSchool school,
            int manaCost,
            IEnumerable<SpellEffectSpec> effects)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("Spell templateId must be a non-empty stable id.", nameof(templateId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Spell displayName must be a non-empty label.", nameof(displayName));
            if (school == MagicSchool.None)
                throw new ArgumentOutOfRangeException(nameof(school), school, "Spell must declare a real school.");
            if (manaCost < 0)
                throw new ArgumentOutOfRangeException(nameof(manaCost), manaCost, "Mana cost must be zero or positive.");
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));

            _effects = ToArray(effects);
            if (_effects.Length == 0)
                throw new ArgumentException("Spell must declare at least one effect spec.", nameof(effects));

            TemplateId = templateId;
            DisplayName = displayName;
            School = school;
            ManaCost = manaCost;
        }

        public string TemplateId { get; }
        public string DisplayName { get; }
        public MagicSchool School { get; }
        public int ManaCost { get; }
        public IReadOnlyList<SpellEffectSpec> Effects => _effects;

        private static SpellEffectSpec[] ToArray(IEnumerable<SpellEffectSpec> effects)
        {
            var buffer = new List<SpellEffectSpec>();
            foreach (var effect in effects)
            {
                buffer.Add(effect);
            }
            return buffer.ToArray();
        }
    }
}
