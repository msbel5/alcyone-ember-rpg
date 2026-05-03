using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the first Sprint 5 effect resolver slice: successful casts may apply only instantaneous
// DirectDamage, RestoreHealth, and RestoreFatigue effects, with all refusal paths leaving target vitals untouched.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic instantaneous spell effect resolution.</summary>
    public sealed class SpellEffectResolutionServiceTests
    {
        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_DamagesTargetWithoutExtraManaSpend()
        {
            var caster = CreateActor(501, "Acolyte", ActorRole.Player, health: 16, mana: 20);
            var target = CreateActor(601, "Ash Rat", ActorRole.Enemy, health: 14, mana: 4);
            var cast = new SpellCastingService().TryCast(
                caster,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId });
            var manaAfterCast = caster.Vitals.Mana.Current;
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.None));
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(8));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(6));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(manaAfterCast));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_ClampsAtZeroHealth()
        {
            var target = CreateActor(601, "Ash Rat", ActorRole.Enemy, health: 5, mana: 4);
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateFlameBolt(), 12, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDamage, Is.EqualTo(5));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_RestoreHealth_HealsTargetUpToMax()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateMendingTouch(), 10, "cast");
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
        public void ResolveInstantaneousEffects_RestoreFatigue_RestoresTargetFatigue()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 6);
            var spell = new SpellDefinition(
                "restore_fatigue_test",
                "Restore Fatigue Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 4, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 5, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(2));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(12));
        }

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
                    new SpellEffectSpec(SpellEffectKind.DirectDamage, 5, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 4, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreHealth, 2, 0),
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
            var failedCast = SpellCastResult.Fail(SpellCastError.InsufficientMana, SliceSpellCatalog.CreateFlameBolt(), "failed");
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
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateMendingTouch(), 10, "cast");
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
        public void ResolveInstantaneousEffects_NonInstantaneousEffect_IsRejectedWithoutMutatingTarget()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateEmberWard(), 15, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.NonInstantaneousEffect));
            Assert.That(result.Spell.TemplateId, Is.EqualTo(SliceSpellCatalog.EmberWardTemplateId));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
        }

        [Test]
        public void ResolveInstantaneousEffects_UnsupportedInstantaneousShieldBuff_IsRejectedWithoutMutatingTarget()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4, fatigue: 7);
            var spell = new SpellDefinition(
                "instant_shield_buff_test",
                "Instant Shield Buff Test",
                MagicSchool.Alteration,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectKind.DirectDamage, 3, 0),
                    new SpellEffectSpec(SpellEffectKind.ShieldBuff, 2, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.UnsupportedEffect));
            Assert.That(result.AppliedEffectCount, Is.EqualTo(0));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(result.TotalRestoredFatigue, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(7));
        }

        private static ActorRecord CreateActor(int id, string name, ActorRole role, int health, int mana, int fatigue = 12)
        {
            return new ActorRecord(
                new ActorId((ulong)id),
                name,
                role,
                new EmberStatBlock(10, 11, 12, 14, 9, 8),
                new ActorVitals(new VitalStat(health, 16), new VitalStat(fatigue, 12), new VitalStat(mana, 20)),
                new GridPosition(1, 1),
                accuracy: 11,
                dodge: 6,
                armor: 1,
                baseDamage: 3);
        }
    }
}
