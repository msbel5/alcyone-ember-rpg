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
    // REF-2 (LEFT-028): split into partial files by category (Restore/DirectMana/DirectFatigue/Validation).
    // This file keeps the shared CreateActor helper and the DirectDamage tests; partials share the helper.
    public sealed partial class SpellEffectResolutionServiceTests
    {
        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_DamagesTargetWithoutExtraManaSpend()
        {
            var caster = CreateActor(501, "Acolyte", ActorRole.Player, health: 16, mana: 20);
            var target = CreateActor(601, "Ash Rat", ActorRole.Enemy, health: 14, mana: 4);
            var cast = new SpellCastingService().TryCast(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId });
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
            var cast = SpellCastResult.Ok(WorldSpellCatalog.CreateFlameBolt(), 12, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDamage, Is.EqualTo(5));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_OvershootMagnitudeClampsAtZeroHealth()
        {
            var target = CreateActor(601, "Ash Rat", ActorRole.Enemy, health: 3, mana: 4);
            var spell = new SpellDefinition(
                "direct_damage_clamp_test",
                "Direct Damage Clamp Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 9, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDamage, Is.EqualTo(3));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(0));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealthAggregatesIndependently()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 10, mana: 4);
            var spell = new SpellDefinition(
                "damage_then_restore_test",
                "Damage Then Restore Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 6, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 3, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDamage, Is.EqualTo(6));
            Assert.That(result.TotalHealing, Is.EqualTo(3));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(7));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_OvershootDamageClampsThenRestoreApplies()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 4, mana: 4);
            var spell = new SpellDefinition(
                "damage_overshoot_then_restore_test",
                "Damage Overshoot Then Restore Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 11, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 5, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDamage, Is.EqualTo(4));
            Assert.That(result.TotalHealing, Is.EqualTo(5));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(5));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_ZeroMagnitudeLeavesHealthUnchanged()
        {
            var target = CreateActor(601, "Ash Rat", ActorRole.Enemy, health: 9, mana: 4);
            var spell = new SpellDefinition(
                "direct_damage_zero_test",
                "Direct Damage Zero Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(9));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroMagnitudeLeavesHealthUnchanged()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 11, mana: 4, fatigue: 9);
            var spell = new SpellDefinition(
                "direct_damage_restore_health_zero_bundle_test",
                "Direct Damage Restore Health Zero Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 0, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 0, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(11));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(4));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(9));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroRestoreLeavesDamageApplied()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 10, mana: 4);
            var spell = new SpellDefinition(
                "direct_damage_restore_health_zero_restore_bundle_test",
                "Direct Damage Restore Health Zero Restore Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 4, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 0, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDamage, Is.EqualTo(4));
            Assert.That(result.TotalHealing, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(6));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectDamage_BundledWithRestoreHealth_ZeroDamageLeavesRestoreApplied()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 10, mana: 4);
            var spell = new SpellDefinition(
                "direct_damage_restore_health_zero_damage_bundle_test",
                "Direct Damage Restore Health Zero Damage Bundle Test",
                MagicSchool.Destruction,
                1,
                new[]
                {
                    new SpellEffectSpec(SpellEffectCode.DirectDamage, 0, 0),
                    new SpellEffectSpec(SpellEffectCode.RestoreHealth, 4, 0),
                });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(2));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(result.TotalHealing, Is.EqualTo(4));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(14));
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
