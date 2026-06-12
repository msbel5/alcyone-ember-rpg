using System.Collections.Generic;
using EmberCrpg.Domain.Magic;

// Design note:
// WorldSpellCatalog is the deterministic Sprint 5 starter spell set.
// Inputs: none beyond fixed template ids and effect specs chosen for the foundation.
// Outputs: stable read-only spell list and lookup, used by SpellCastingService and tests.
// Bible reference: EMBER_VISION_BIBLE.md §8 Sprint Mapping (Sprint 5 magic foundation),
// MASTER_MECHANICS_BIBLE.md §14-§15 (school + effect taxonomy).
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Static deterministic catalog for the Sprint 5 starter spells.</summary>
    public static class WorldSpellCatalog
    {
        public const string FlameBoltTemplateId = "flame_bolt";
        public const string MendingTouchTemplateId = "mending_touch";
        public const string EmberWardTemplateId = "ember_ward";

        private static readonly SpellDefinition[] _spells = BuildSpells();
        private static readonly Dictionary<string, SpellDefinition> _byId = BuildLookup(_spells);
        // PR#13 bot review fix: wrap the raw array in ReadOnlyCollection so a
        // caller can't downcast All back to SpellDefinition[] and mutate the
        // shared catalog (e.g. swap or null out a slot).
        private static readonly System.Collections.ObjectModel.ReadOnlyCollection<SpellDefinition> _allView =
            new System.Collections.ObjectModel.ReadOnlyCollection<SpellDefinition>(_spells);

        public static IReadOnlyList<SpellDefinition> All => _allView;

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

        // ----- F28: the spell school grows to EIGHT — three damage types, shield, heal, light,
        // haste and a recall gate. "light"/"haste"/"recall" are OPEN-SET effect codes: the pure
        // resolver consumes mana and no-ops them; the presentation layer reacts by template id.
        public const string FrostLanceTemplateId = "frost_lance";
        public const string SparkArcTemplateId = "spark_arc";
        public const string LanternGlowTemplateId = "lantern_glow";
        public const string WindStepTemplateId = "wind_step";
        public const string RecallGateTemplateId = "recall_gate";

        public static SpellDefinition CreateFrostLance()
        {
            return new SpellDefinition(
                FrostLanceTemplateId,
                "Frost Lance",
                MagicSchool.Destruction,
                SpellTargetKind.SingleTarget,
                manaCost: 17, // the flame curve: ceil(11 dmg * 3/2) — the estimator is the floor
                rangeInTiles: 7,
                cooldownTicks: 9,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 11, 0) });
        }

        public static SpellDefinition CreateSparkArc()
        {
            return new SpellDefinition(
                SparkArcTemplateId,
                "Spark Arc",
                MagicSchool.Destruction,
                SpellTargetKind.SingleTarget,
                manaCost: 9, // the flame curve: ceil(6 dmg * 3/2) — the estimator is the floor
                rangeInTiles: 9,
                cooldownTicks: 3,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 6, 0) });
        }

        public static SpellDefinition CreateLanternGlow()
        {
            return new SpellDefinition(
                LanternGlowTemplateId,
                "Lantern Glow",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 6,
                rangeInTiles: 0,
                cooldownTicks: 20,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.FromCode("light"), 60, 60) });
        }

        public static SpellDefinition CreateWindStep()
        {
            return new SpellDefinition(
                WindStepTemplateId,
                "Wind Step",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 12,
                rangeInTiles: 0,
                cooldownTicks: 30,
                effects: new[]
                {
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 10, 0), // real wind in the legs
                    new SpellEffectSpec(SpellEffectCode.FromCode("haste"), 30, 30),
                });
        }

        public static SpellDefinition CreateRecallGate()
        {
            return new SpellDefinition(
                RecallGateTemplateId,
                "Recall Gate",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 20,
                rangeInTiles: 0,
                cooldownTicks: 60,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.FromCode("recall"), 1, 0) });
        }

        private static SpellDefinition[] BuildSpells()
        {
            return new[]
            {
                CreateFlameBolt(),
                CreateMendingTouch(),
                CreateEmberWard(),
                CreateFrostLance(),
                CreateSparkArc(),
                CreateLanternGlow(),
                CreateWindStep(),
                CreateRecallGate(),
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
