// REF-2 (LEFT-028): Validation/edge tests split out of SpellEffectResolutionServiceTests.cs (partial, same assertions).
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
        public void ResolveInstantaneousEffects_MultipleSupportedEffects_AppliesInDefinitionOrder()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 16, mana: 4, fatigue: 9);
            var spell = new SpellDefinition(
                "scorching_rebuke_test",
                "Scorching Rebuke Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 5, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 4, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 2, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(3));
            Assert.That(result.TotalDamage, Is.EqualTo(5));
            Assert.That(result.TotalHealing, Is.EqualTo(2));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(3));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(12));
        }

        [Test]
        public void ResolveInstantaneousEffects_NullCast_IsRejectedWithoutMutatingTarget()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(null, target);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.InvalidCast));
            Assert.That(result.Spell, Is.Null);
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
        }

        [Test]
        public void ResolveInstantaneousEffects_FailedCast_IsRejectedWithoutMutatingTarget()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var failedCast = SpellCastResult.Fail(SpellCastError.InsufficientMana, WorldSpellCatalog.CreateFlameBolt(), "failed");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(failedCast, target);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.InvalidCast));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
        }

        [Test]
        public void ResolveInstantaneousEffects_NullOrIncapacitatedTarget_IsRejected()
        {
            var deadTarget = CreateActor(601, "Guard", ActorRole.Guard, health: 0, mana: 4);
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateMendingTouch(), 10, "cast");
            var service = new SpellEffectResolutionService();

            var nullTargetResult = service.ResolveInstantaneousEffects(cast, null);
            var deadTargetResult = service.ResolveInstantaneousEffects(cast, deadTarget);

            Assert.That(nullTargetResult.Success, Is.False);
            Assert.That(nullTargetResult.Error, Is.EqualTo(SpellEffectResolutionError.InvalidTarget));
            Assert.That(deadTargetResult.Success, Is.False);
            Assert.That(deadTargetResult.Error, Is.EqualTo(SpellEffectResolutionError.InvalidTarget));
            Assert.That(deadTarget.Vitals.Health.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_TimedEffect_IsSkippedWithoutMutatingTarget()
        {
            // F28 contract change: a timed effect (ember_ward's shield) no longer REJECTS the
            // whole resolution — it is SKIPPED (the buff systems own it) and the cast commits.
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateEmberWard(), 15, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(0));
            Assert.That(result.Spell.TemplateId, Is.EqualTo(WorldSpellCatalog.EmberWardTemplateId));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
        }

        [Test]
        public void ResolveInstantaneousEffects_UnsupportedShieldBuff_IsSkippedWhileSupportedSubsetApplies()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4, fatigue: 7);
            var spell = new SpellDefinition(
                "instant_shield_buff_test",
                "Instant Shield Buff Test",
                MagicSchool.Alteration,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 3, 0),
                    new SpellEffectSpec(SpellEffectCode.ShieldBuff, 2, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            // F28 contract change: the unsupported ShieldBuff row is skipped (not a rejection);
            // the supported DirectDamage row still lands. Mixed spells resolve their legal subset.
            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(3));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(10));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(7));
        }

        [Test]
        public void ResolveInstantaneousEffects_AllSixSupportedKindsBundledAggregateIndependently()
        {
            // Pins compositional correctness once the instantaneous matrix is complete:
            // DirectDamage/RestoreHealth, DirectFatigue/RestoreFatigue, DirectMana/RestoreMana
            // applied in one spell preserve six independent counters and final pool states.
            var target = CreateActor(601, "AllSixTarget", ActorRole.Guard, health: 10, mana: 10, fatigue: 8);
            var spell = new SpellDefinition(
                "all_six_instantaneous_bundle_test",
                "All Six Instantaneous Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 3, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 4, 0),
                    new SpellEffectSpec(SpellEffectCode.DirectFatigue, 4, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 3, 0),
                    new SpellEffectSpec(SpellEffectCode.DirectMana, 5, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreMana, 6, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(6));
            Assert.That(result.TotalDamage, Is.EqualTo(3));
            Assert.That(result.TotalHealing, Is.EqualTo(4));
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(4));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(3));
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(5));
            Assert.That(result.TotalRestoredMana, Is.EqualTo(6));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(11));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(7));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(11));
        }
    }
}
