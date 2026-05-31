// REF-2 (LEFT-028): Restore (health/fatigue/mana) tests split out of SpellEffectResolutionServiceTests.cs (partial, same assertions).
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Magic
{
    public sealed partial class SpellEffectResolutionServiceTests
    {
        [Test]
        public void ResolveInstantaneousEffects_RestoreHealth_HealsTargetUpToMax()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateMendingTouch(), 10, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(3));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreHealth_ZeroMagnitudeLeavesHealthUnchanged()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var spell = new SpellDefinition(
                "restore_health_zero_test",
                "Restore Health Zero Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreHealth, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreHealth_ClampsAtMaxHealth()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 14, mana: 4);
            var spell = new SpellDefinition(
                "restore_health_clamp_test",
                "Restore Health Clamp Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreHealth, 5, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalHealing, Is.EqualTo(2));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreFatigue_RestoresTargetFatigue()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 6);
            var spell = new SpellDefinition(
                "restore_fatigue_test",
                "Restore Fatigue Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 4, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(4));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(10));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreFatigue_ClampsAtMaxFatigue()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 10);
            var spell = new SpellDefinition(
                "restore_fatigue_clamp_test",
                "Restore Fatigue Clamp Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 5, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(2));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(12));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 7);
            var spell = new SpellDefinition(
                "restore_fatigue_zero_test",
                "Restore Fatigue Zero Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(7));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreMana_RestoresTargetMana()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 6, fatigue: 12);
            var spell = new SpellDefinition(
                "restore_mana_test",
                "Restore Mana Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreMana, 7, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(7));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(13));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(12));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreMana_ClampsAtMaxMana()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 17, fatigue: 12);
            var spell = new SpellDefinition(
                "restore_mana_clamp_test",
                "Restore Mana Clamp Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreMana, 8, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalRestoredMana, Is.EqualTo(3));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(20));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreMana_BundledWithOtherEffectsAggregatesPerKind()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 12, mana: 5, fatigue: 9);
            var spell = new SpellDefinition(
                "ember_communion_test",
                "Ember Communion Test",
                MagicSchool.Restoration,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.RestoreMana, 6, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 2, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 3, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(3));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(3));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(2));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(6));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(15));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(11));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(11));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreMana_ZeroMagnitudeLeavesManaUnchanged()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 8);
            var spell = new SpellDefinition(
                "restore_mana_zero_test",
                "Restore Mana Zero Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.RestoreMana, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalRestoredMana, Is.EqualTo(0));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(8));
        }
    }
}
