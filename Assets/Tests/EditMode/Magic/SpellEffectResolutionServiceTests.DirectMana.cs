// REF-2 (LEFT-028): DirectMana tests split out of SpellEffectResolutionServiceTests.cs (partial, same assertions).
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
        public void ResolveInstantaneousEffects_DirectMana_DrainsTargetMana()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 14, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_mana_test",
                "Direct Mana Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectMana, 5, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(0));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(5));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(9));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(12));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_ClampsAtZeroMana()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 3, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_mana_clamp_test",
                "Direct Mana Clamp Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectMana, 9, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(3));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_OvershootMagnitudeClampsAtZeroMana()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 2, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_mana_overshoot_test",
                "Direct Mana Overshoot Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectMana, 11, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(2));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreManaAggregatesIndependently()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 10, fatigue: 12);
            var spell = new SpellDefinition(
                "mana_burn_then_restore_test",
                "Mana Burn Then Restore Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectMana, 6, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreMana, 3, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(6));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(3));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(7));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_OvershootDrainClampsThenRestoreApplies()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 4, fatigue: 12);
            var spell = new SpellDefinition(
                "mana_overshoot_then_restore_test",
                "Mana Overshoot Then Restore Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectMana, 11, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreMana, 5, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(4));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(5));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(5));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_ZeroMagnitudeLeavesManaUnchanged()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 8);
            var spell = new SpellDefinition(
                "direct_mana_zero_test",
                "Direct Mana Zero Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectMana, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(0));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(8));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroMagnitudeLeavesManaUnchanged()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 8, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_mana_restore_mana_zero_bundle_test",
                "Direct Mana Restore Mana Zero Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectMana, 0, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreMana, 0, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(0));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(0));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(8));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(12));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroRestoreLeavesDrainApplied()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 10, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_mana_restore_mana_zero_restore_bundle_test",
                "Direct Mana Restore Mana Zero Restore Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectMana, 4, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreMana, 0, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(4));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(0));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(6));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_BundledWithRestoreMana_ZeroDrainLeavesRestoreApplied()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 6, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_mana_restore_mana_zero_drain_bundle_test",
                "Direct Mana Restore Mana Zero Drain Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectMana, 0, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreMana, 4, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(0));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(4));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(10));
        }
    }
}
