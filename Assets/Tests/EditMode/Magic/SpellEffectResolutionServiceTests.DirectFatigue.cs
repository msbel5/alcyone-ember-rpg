// REF-2 (LEFT-028): DirectFatigue tests split out of SpellEffectResolutionServiceTests.cs (partial, same assertions).
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
        public void ResolveInstantaneousEffects_DirectFatigue_DrainsTargetFatigue()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_fatigue_test",
                "Direct Fatigue Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectFatigue, 5, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(0));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(0));
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(5));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(7));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(4));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_ClampsAtZeroFatigue()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 3);
            var spell = new SpellDefinition(
                "direct_fatigue_clamp_test",
                "Direct Fatigue Clamp Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectFatigue, 9, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(3));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_OvershootMagnitudeClampsAtZeroFatigue()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 2);
            var spell = new SpellDefinition(
                "direct_fatigue_overshoot_test",
                "Direct Fatigue Overshoot Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectFatigue, 11, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(2));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigueAggregatesIndependently()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 10);
            var spell = new SpellDefinition(
                "fatigue_burn_then_restore_test",
                "Fatigue Burn Then Restore Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectFatigue, 6, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 3, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(6));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(3));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(7));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_OvershootDrainClampsThenRestoreApplies()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 4);
            var spell = new SpellDefinition(
                "fatigue_overshoot_then_restore_test",
                "Fatigue Overshoot Then Restore Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectFatigue, 11, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 5, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(4));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(5));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(5));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_ZeroMagnitudeLeavesFatigueUnchanged()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 8);
            var spell = new SpellDefinition(
                "direct_fatigue_zero_test",
                "Direct Fatigue Zero Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectFatigue, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(0));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(8));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 8);
            var spell = new SpellDefinition(
                "direct_fatigue_restore_fatigue_zero_bundle_test",
                "Direct Fatigue Restore Fatigue Zero Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectFatigue, 0, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 0, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(8));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(16));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(4));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_BundledWithRestoreFatigue_ZeroDrainLeavesRestoreApplied()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 6);
            var spell = new SpellDefinition(
                "direct_fatigue_restore_fatigue_zero_drain_bundle_test",
                "Direct Fatigue Restore Fatigue Zero Drain Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectFatigue, 0, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 4, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(4));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(10));
        }
    }
}
