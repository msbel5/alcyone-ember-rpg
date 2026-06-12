using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 shield-buff application slice: a successful cast can write its
// timed ShieldBuff effects into a ShieldBuffState container. F28 contract change: the
// instantaneous resolver now SKIPS timed rows instead of rejecting them, so both services can
// act on the same cast.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies SpellEffectResolutionService.ApplyShieldBuffs deterministic behavior.</summary>
    public sealed class ShieldBuffApplicationServiceTests
    {
        [Test]
        public void ApplyShieldBuffs_EmberWardCast_RecordsBuffWithMagnitudeAndDuration()
        {
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateEmberWard(), 15, "cast");
            var state = new ShieldBuffState();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, state);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.None));
            Assert.That(result.AppliedBuffCount, Is.EqualTo(1));
            Assert.That(result.TotalAppliedMagnitude, Is.EqualTo(4));
            Assert.That(result.TotalAppliedDurationTicks, Is.EqualTo(30));
            Assert.That(result.Spell.TemplateId, Is.EqualTo(WorldSpellCatalog.EmberWardTemplateId));
            Assert.That(state.IsActive(WorldSpellCatalog.EmberWardTemplateId), Is.True);
            Assert.That(state.GetRemainingTicks(WorldSpellCatalog.EmberWardTemplateId), Is.EqualTo(30));
            Assert.That(state.GetMagnitude(WorldSpellCatalog.EmberWardTemplateId), Is.EqualTo(4));
        }

        [Test]
        public void ApplyShieldBuffs_EmberWardCastReplacingActiveBuff_OverwritesEntry()
        {
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateEmberWard(), 15, "cast");
            var state = new ShieldBuffState();
            state.SetActiveBuff(WorldSpellCatalog.EmberWardTemplateId, remainingTicks: 5, magnitude: 1);
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, state);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(1));
            Assert.That(state.GetRemainingTicks(WorldSpellCatalog.EmberWardTemplateId), Is.EqualTo(30));
            Assert.That(state.GetMagnitude(WorldSpellCatalog.EmberWardTemplateId), Is.EqualTo(4));
        }

        [Test]
        public void ApplyShieldBuffs_MultipleTimedBuffs_AppliesAllInDefinitionOrder()
        {
            var spell = new SpellDefinition(
                "twin_ward_test",
                "Twin Ward Test",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 1,
                rangeInTiles: 0,
                cooldownTicks: 0,
                effects: new[]
                {
                    new SpellEffectSpec(SpellEffectCode.ShieldBuff, 3, 12),
                    new SpellEffectSpec(SpellEffectCode.ShieldBuff, 5, 18),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var state = new ShieldBuffState();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, state);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(2));
            Assert.That(result.TotalAppliedMagnitude, Is.EqualTo(8));
            Assert.That(result.TotalAppliedDurationTicks, Is.EqualTo(30));
            Assert.That(state.GetRemainingTicks("twin_ward_test"), Is.EqualTo(18));
            Assert.That(state.GetMagnitude("twin_ward_test"), Is.EqualTo(5));
        }

        [Test]
        public void ApplyShieldBuffs_MixedSpell_OnlyWritesTimedBuffEffects()
        {
            var spell = new SpellDefinition(
                "scorch_and_ward_test",
                "Scorch and Ward Test",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 1,
                rangeInTiles: 0,
                cooldownTicks: 0,
                effects: new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 6, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 4, 0),
                    new SpellEffectSpec(SpellEffectCode.ShieldBuff, 4, 30),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var state = new ShieldBuffState();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, state);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(1));
            Assert.That(result.TotalAppliedMagnitude, Is.EqualTo(4));
            Assert.That(result.TotalAppliedDurationTicks, Is.EqualTo(30));
            Assert.That(state.GetRemainingTicks("scorch_and_ward_test"), Is.EqualTo(30));
            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplyShieldBuffs_InstantaneousShieldBuffEffect_IsIgnoredAsBuff()
        {
            var spell = new SpellDefinition(
                "instant_shield_buff_only_test",
                "Instant Shield Buff Only Test",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 1,
                rangeInTiles: 0,
                cooldownTicks: 0,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.ShieldBuff, 5, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var state = new ShieldBuffState();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, state);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(0));
            Assert.That(result.TotalAppliedMagnitude, Is.EqualTo(0));
            Assert.That(result.TotalAppliedDurationTicks, Is.EqualTo(0));
            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyShieldBuffs_FlameBoltCast_ProducesNoBuffWritesAndStateStaysEmpty()
        {
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateFlameBolt(), 12, "cast");
            var state = new ShieldBuffState();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, state);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(0));
            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyShieldBuffs_NullCast_IsRejectedAndStateIsUntouched()
        {
            var state = new ShieldBuffState();
            state.SetActiveBuff(WorldSpellCatalog.EmberWardTemplateId, remainingTicks: 7, magnitude: 2);
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(null, state);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.InvalidCast));
            Assert.That(result.Spell, Is.Null);
            Assert.That(state.GetRemainingTicks(WorldSpellCatalog.EmberWardTemplateId), Is.EqualTo(7));
            Assert.That(state.GetMagnitude(WorldSpellCatalog.EmberWardTemplateId), Is.EqualTo(2));
        }

        [Test]
        public void ApplyShieldBuffs_FailedCast_IsRejectedAndStateIsUntouched()
        {
            var state = new ShieldBuffState();
            var failedCast = SpellCastResult.Fail(SpellCastError.InsufficientMana, WorldSpellCatalog.CreateEmberWard(), "failed");
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(failedCast, state);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.InvalidCast));
            Assert.That(state.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyShieldBuffs_NullShieldBuffState_IsRejectedWithInvalidBuffState()
        {
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateEmberWard(), 15, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.InvalidBuffState));
            Assert.That(result.Spell.TemplateId, Is.EqualTo(WorldSpellCatalog.EmberWardTemplateId));
        }

        [Test]
        public void ApplyShieldBuffs_AndInstantaneousResolutionSkipsTheTimedShield()
        {
            // F28 contract change: the instantaneous resolver SKIPS the timed shield row instead
            // of rejecting — both services act on the same cast without stepping on each other.
            var target = CreateGuard();
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateEmberWard(), 15, "cast");
            var state = new ShieldBuffState();
            var service = new SpellEffectResolutionService();

            var buffApplication = service.ApplyShieldBuffs(cast, state);
            var instantaneous = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(buffApplication.Success, Is.True);
            Assert.That(state.IsActive(WorldSpellCatalog.EmberWardTemplateId), Is.True);
            Assert.That(instantaneous.Success, Is.True);
            Assert.That(instantaneous.AppliedEffectCount, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(13));
        }

        private static ActorRecord CreateGuard()
        {
            return new ActorRecord(
                new ActorId(601),
                "Guard",
                ActorRole.Guard,
                new EmberStatBlock(10, 11, 12, 14, 9, 8),
                new ActorVitals(new VitalStat(13, 16), new VitalStat(12, 12), new VitalStat(4, 20)),
                new GridPosition(1, 1),
                accuracy: 11,
                dodge: 6,
                armor: 1,
                baseDamage: 3);
        }
    }
}
