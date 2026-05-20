using System.Collections.Generic;
using EmberCrpg.Domain.Magic;

// Design note:
// SliceSpellCatalog is the deterministic Sprint 5 starter spell set.
// Inputs: none beyond fixed template ids and effect specs chosen for the foundation.
// Outputs: stable read-only spell list and lookup, used by SpellCastingService and tests.
// Bible reference: EMBER_VISION_BIBLE.md §8 Sprint Mapping (Sprint 5 magic foundation),
// MASTER_MECHANICS_BIBLE.md §14-§15 (school + effect taxonomy).
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Static deterministic catalog for the Sprint 5 starter spells.</summary>
    public static class SliceSpellCatalog
    {
        public const string FlameBoltTemplateId = "flame_bolt";
        public const string MendingTouchTemplateId = "mending_touch";
        public const string EmberWardTemplateId = "ember_ward";

        private static readonly SpellDefinition[] _spells = BuildSpells();
        private static readonly Dictionary<string, SpellDefinition> _byId = BuildLookup(_spells);

        public static IReadOnlyList<SpellDefinition> All => _spells;

        public static SpellDefinition Find(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return null;
            return _byId.TryGetValue(templateId, out var spell) ? spell : null;
        }

        public const int FlameBoltRangeInTiles = 8;

        // Catalog cooldowns wire the existing SpellCooldownService to the deterministic Sprint 5
        // starter spells. EmberWard's cooldown matches its own buff duration so the buff cannot
        // legally double-stack via back-to-back self-recast.
        public const int FlameBoltCooldownTicks = 6;
        public const int MendingTouchCooldownTicks = 4;
        public const int EmberWardCooldownTicks = 30;

        public static SpellDefinition CreateFlameBolt()
        {
            return new SpellDefinition(
                FlameBoltTemplateId,
                "Flame Bolt",
                MagicSchool.Destruction,
                SpellTargetKind.SingleTarget,
                manaCost: 12,
                rangeInTiles: FlameBoltRangeInTiles,
                cooldownTicks: FlameBoltCooldownTicks,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 8, 0) });
        }

        public static SpellDefinition CreateMendingTouch()
        {
            return new SpellDefinition(
                MendingTouchTemplateId,
                "Mending Touch",
                MagicSchool.Restoration,
                SpellTargetKind.Touch,
                manaCost: 10,
                rangeInTiles: 0,
                cooldownTicks: MendingTouchCooldownTicks,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.RestoreHealth, 6, 0) });
        }

        public static SpellDefinition CreateEmberWard()
        {
            return new SpellDefinition(
                EmberWardTemplateId,
                "Ember Ward",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 15,
                rangeInTiles: 0,
                cooldownTicks: EmberWardCooldownTicks,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.ShieldBuff, 4, 30) });
        }

        private static SpellDefinition[] BuildSpells()
        {
            return new[]
            {
                CreateFlameBolt(),
                CreateMendingTouch(),
                CreateEmberWard(),
            };
        }

        private static Dictionary<string, SpellDefinition> BuildLookup(SpellDefinition[] spells)
        {
            var lookup = new Dictionary<string, SpellDefinition>(spells.Length);
            foreach (var spell in spells)
            {
                lookup.Add(spell.TemplateId, spell);
            }
            return lookup;
        }
    }
}
