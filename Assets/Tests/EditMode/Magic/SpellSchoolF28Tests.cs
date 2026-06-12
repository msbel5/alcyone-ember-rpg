using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>
    /// F28: the spell school is EIGHT — three damage types, shield, heal, light, haste, recall.
    /// Effect tests: the new damage spells mutate vitals through the SAME resolution service the
    /// live cast uses; wind_step restores fatigue; the OPEN-SET codes (light/haste/recall) resolve
    /// safely without touching vitals (their world half lives in the presentation cast path).
    /// </summary>
    public sealed class SpellSchoolF28Tests
    {
        [Test]
        public void Catalog_HasEightSpells_CoveringTheSchool()
        {
            Assert.That(WorldSpellCatalog.All.Count, Is.EqualTo(8));
            string[] expected =
            {
                WorldSpellCatalog.FlameBoltTemplateId, WorldSpellCatalog.MendingTouchTemplateId,
                WorldSpellCatalog.EmberWardTemplateId, WorldSpellCatalog.FrostLanceTemplateId,
                WorldSpellCatalog.SparkArcTemplateId, WorldSpellCatalog.LanternGlowTemplateId,
                WorldSpellCatalog.WindStepTemplateId, WorldSpellCatalog.RecallGateTemplateId,
            };
            foreach (var id in expected)
                Assert.That(WorldSpellCatalog.Find(id), Is.Not.Null, id);

            int damageTypes = 0;
            foreach (var spell in WorldSpellCatalog.All)
                foreach (var effect in spell.Effects)
                    if (effect.Kind == SpellEffectCode.DirectDamage) damageTypes++;
            Assert.That(damageTypes, Is.EqualTo(3), "flame + frost + spark are the three damage types");
        }

        [Test]
        public void FrostLance_AndSparkArc_DealTheirDamage()
        {
            var resolver = new SpellEffectResolutionService();

            var frostTarget = Dummy(40);
            resolver.ResolveInstantaneousEffects(
                SpellCastResult.Ok(WorldSpellCatalog.CreateFrostLance(), 17, "cast"), frostTarget);
            Assert.That(frostTarget.Vitals.Health.Current, Is.EqualTo(40 - 11));

            var sparkTarget = Dummy(40);
            resolver.ResolveInstantaneousEffects(
                SpellCastResult.Ok(WorldSpellCatalog.CreateSparkArc(), 9, "cast"), sparkTarget);
            Assert.That(sparkTarget.Vitals.Health.Current, Is.EqualTo(40 - 6));
        }

        [Test]
        public void WindStep_RestoresFatigue()
        {
            var caster = Dummy(40);
            caster.ApplyVitals(new ActorVitals(
                caster.Vitals.Health,
                new VitalStat(5, caster.Vitals.Fatigue.Max),
                caster.Vitals.Mana));

            new SpellEffectResolutionService().ResolveInstantaneousEffects(
                SpellCastResult.Ok(WorldSpellCatalog.CreateWindStep(), 12, "cast"), caster);

            Assert.That(caster.Vitals.Fatigue.Current, Is.EqualTo(15), "wind in the legs: +10 fatigue");
        }

        [Test]
        public void OpenSetCodes_LightAndRecall_ResolveSafely_WithoutVitalChange()
        {
            var resolver = new SpellEffectResolutionService();
            var caster = Dummy(40);
            int hp = caster.Vitals.Health.Current;
            int fat = caster.Vitals.Fatigue.Current;
            int mana = caster.Vitals.Mana.Current;

            Assert.DoesNotThrow(() => resolver.ResolveInstantaneousEffects(
                SpellCastResult.Ok(WorldSpellCatalog.CreateLanternGlow(), 6, "cast"), caster));
            Assert.DoesNotThrow(() => resolver.ResolveInstantaneousEffects(
                SpellCastResult.Ok(WorldSpellCatalog.CreateRecallGate(), 20, "cast"), caster));

            Assert.That(caster.Vitals.Health.Current, Is.EqualTo(hp));
            Assert.That(caster.Vitals.Fatigue.Current, Is.EqualTo(fat));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(mana),
                "the open-set codes are world-side; the resolver must not touch vitals");
        }

        private static ActorRecord Dummy(int health)
        {
            return new ActorRecord(
                new ActorId(900UL),
                "Spell Dummy",
                ActorRole.Enemy,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(health, health), new VitalStat(20, 20), new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 10,
                armor: 0,
                baseDamage: 1);
        }
    }
}
