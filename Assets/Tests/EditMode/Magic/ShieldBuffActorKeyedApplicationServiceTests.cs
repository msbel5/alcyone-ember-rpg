using System;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the deterministic Sprint 5 actor-keyed shield-buff application overload:
// SpellEffectResolutionService.ApplyShieldBuffs(SpellCastResult, ShieldBuffStateRegistry, actorId)
// routes a successful cast's timed shield-buff effects into the per-actor bag owned by
// ShieldBuffStateRegistry.GetOrCreate(actorId), delegating to the existing single-bag overload.
// This is glue-only: no new application/decay/save/absorption rules.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies the actor-keyed registry overload of ApplyShieldBuffs.</summary>
    public sealed class ShieldBuffActorKeyedApplicationServiceTests
    {
        private const string CasterActorId = "actor.caster.1";
        private const string OtherActorId = "actor.caster.2";

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_EmberWardCast_WritesIntoOwnActorBag()
        {
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateEmberWard(), 15, "cast");
            var registry = new ShieldBuffStateRegistry();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, registry, CasterActorId);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.None));
            Assert.That(result.AppliedBuffCount, Is.EqualTo(1));
            Assert.That(result.TotalAppliedMagnitude, Is.EqualTo(4));
            Assert.That(result.TotalAppliedDurationTicks, Is.EqualTo(30));

            Assert.That(registry.HasState(CasterActorId), Is.True);
            var casterBag = registry.GetOrNull(CasterActorId);
            Assert.That(casterBag, Is.Not.Null);
            Assert.That(casterBag.IsActive(SliceSpellCatalog.EmberWardTemplateId), Is.True);
            Assert.That(casterBag.GetRemainingTicks(SliceSpellCatalog.EmberWardTemplateId), Is.EqualTo(30));
            Assert.That(casterBag.GetMagnitude(SliceSpellCatalog.EmberWardTemplateId), Is.EqualTo(4));
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_OnlyTouchesTargetActorBag()
        {
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateEmberWard(), 15, "cast");
            var registry = new ShieldBuffStateRegistry();
            var preExisting = registry.GetOrCreate(OtherActorId);
            preExisting.SetActiveBuff("preexisting.buff", remainingTicks: 11, magnitude: 2);
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, registry, CasterActorId);

            Assert.That(result.Success, Is.True);
            Assert.That(registry.HasState(CasterActorId), Is.True);
            Assert.That(registry.HasState(OtherActorId), Is.True);

            var otherBag = registry.GetOrNull(OtherActorId);
            Assert.That(otherBag, Is.Not.Null);
            Assert.That(otherBag.IsActive(SliceSpellCatalog.EmberWardTemplateId), Is.False);
            Assert.That(otherBag.GetRemainingTicks("preexisting.buff"), Is.EqualTo(11));
            Assert.That(otherBag.GetMagnitude("preexisting.buff"), Is.EqualTo(2));
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_RecastReplacesEntryOnSameActorBag()
        {
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateEmberWard(), 15, "cast");
            var registry = new ShieldBuffStateRegistry();
            var existing = registry.GetOrCreate(CasterActorId);
            existing.SetActiveBuff(SliceSpellCatalog.EmberWardTemplateId, remainingTicks: 5, magnitude: 1);
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, registry, CasterActorId);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(1));
            var bag = registry.GetOrNull(CasterActorId);
            Assert.That(bag.GetRemainingTicks(SliceSpellCatalog.EmberWardTemplateId), Is.EqualTo(30));
            Assert.That(bag.GetMagnitude(SliceSpellCatalog.EmberWardTemplateId), Is.EqualTo(4));
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_MixedSpell_OnlyWritesTimedBuffEffectIntoActorBag()
        {
            var spell = new SpellDefinition(
                "scorch_and_ward_actor_test",
                "Scorch and Ward Actor Test",
                MagicSchool.Alteration,
                SpellTargetKind.CasterSelf,
                manaCost: 1,
                rangeInTiles: 0,
                cooldownTicks: 0,
                effects: new[]
                {
                    new SpellEffectSpec(SpellEffectKind.DirectDamage, 6, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreHealth, 4, 0),
                    new SpellEffectSpec(SpellEffectKind.ShieldBuff, 4, 30),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var registry = new ShieldBuffStateRegistry();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, registry, CasterActorId);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(1));
            Assert.That(result.TotalAppliedMagnitude, Is.EqualTo(4));
            Assert.That(result.TotalAppliedDurationTicks, Is.EqualTo(30));
            var bag = registry.GetOrNull(CasterActorId);
            Assert.That(bag, Is.Not.Null);
            Assert.That(bag.GetTrackedSpellTemplateIds().Count, Is.EqualTo(1));
            Assert.That(bag.GetRemainingTicks("scorch_and_ward_actor_test"), Is.EqualTo(30));
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_FlameBoltCast_ProducesNoBuffWritesAndStillCreatesEmptyBag()
        {
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateFlameBolt(), 12, "cast");
            var registry = new ShieldBuffStateRegistry();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(cast, registry, CasterActorId);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedBuffCount, Is.EqualTo(0));
            // Lazy creation occurs once the cast guard passes; the bag is empty.
            Assert.That(registry.HasState(CasterActorId), Is.True);
            var bag = registry.GetOrNull(CasterActorId);
            Assert.That(bag.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_NullCast_IsRejectedAndRegistryIsUntouched()
        {
            var registry = new ShieldBuffStateRegistry();
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(null, registry, CasterActorId);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.InvalidCast));
            Assert.That(result.Spell, Is.Null);
            Assert.That(registry.HasState(CasterActorId), Is.False);
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_FailedCast_IsRejectedAndRegistryIsUntouched()
        {
            var registry = new ShieldBuffStateRegistry();
            var failed = SpellCastResult.Fail(SpellCastError.InsufficientMana, SliceSpellCatalog.CreateEmberWard(), "failed");
            var service = new SpellEffectResolutionService();

            var result = service.ApplyShieldBuffs(failed, registry, CasterActorId);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellEffectResolutionError.InvalidCast));
            Assert.That(registry.HasState(CasterActorId), Is.False);
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_NullRegistry_ThrowsArgumentNull()
        {
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateEmberWard(), 15, "cast");
            var service = new SpellEffectResolutionService();

            Assert.Throws<ArgumentNullException>(() => service.ApplyShieldBuffs(cast, null, CasterActorId));
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_WhitespaceActorId_ThrowsArgument()
        {
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateEmberWard(), 15, "cast");
            var registry = new ShieldBuffStateRegistry();
            var service = new SpellEffectResolutionService();

            Assert.Throws<ArgumentException>(() => service.ApplyShieldBuffs(cast, registry, "   "));
            Assert.That(registry.HasState("   "), Is.False);
            Assert.That(registry.GetTrackedActorIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyShieldBuffs_RegistryOverload_ParityWithSingleBagOverloadOnSameInputState()
        {
            var cast = SpellCastResult.Ok(SliceSpellCatalog.CreateEmberWard(), 15, "cast");
            var service = new SpellEffectResolutionService();

            var registry = new ShieldBuffStateRegistry();
            var registryResult = service.ApplyShieldBuffs(cast, registry, CasterActorId);

            var directBag = new ShieldBuffState();
            var directResult = service.ApplyShieldBuffs(cast, directBag);

            Assert.That(registryResult.Success, Is.EqualTo(directResult.Success));
            Assert.That(registryResult.Error, Is.EqualTo(directResult.Error));
            Assert.That(registryResult.AppliedBuffCount, Is.EqualTo(directResult.AppliedBuffCount));
            Assert.That(registryResult.TotalAppliedMagnitude, Is.EqualTo(directResult.TotalAppliedMagnitude));
            Assert.That(registryResult.TotalAppliedDurationTicks, Is.EqualTo(directResult.TotalAppliedDurationTicks));

            var bag = registry.GetOrNull(CasterActorId);
            Assert.That(bag.GetRemainingTicks(SliceSpellCatalog.EmberWardTemplateId),
                Is.EqualTo(directBag.GetRemainingTicks(SliceSpellCatalog.EmberWardTemplateId)));
            Assert.That(bag.GetMagnitude(SliceSpellCatalog.EmberWardTemplateId),
                Is.EqualTo(directBag.GetMagnitude(SliceSpellCatalog.EmberWardTemplateId)));
        }
    }
}
