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
        public void ResolveInstantaneousEffects_DirectDamage_OvershootMagnitudeClampsAtZeroHealth()
        {
            var target = CreateActor(601, "Ash Rat", ActorRole.Enemy, health: 3, mana: 4);
            var spell = new SpellDefinition(
                "direct_damage_clamp_test",
                "Direct Damage Clamp Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 9, 0) });
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
                    new SpellEffectSpec(SpellEffectKind.DirectDamage, 6, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreHealth, 3, 0),
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
                    new SpellEffectSpec(SpellEffectKind.DirectDamage, 11, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreHealth, 5, 0),
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
                new[] { new SpellEffectSpec(SpellEffectKind.DirectDamage, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedEffectCount, Is.EqualTo(1));
            Assert.That(result.TotalDamage, Is.EqualTo(0));
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(9));
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
        public void ResolveInstantaneousEffects_RestoreHealth_ZeroMagnitudeLeavesHealthUnchanged()
        {
            var target = CreateActor(601, "Guard", ActorRole.Guard, health: 13, mana: 4);
            var spell = new SpellDefinition(
                "restore_health_zero_test",
                "Restore Health Zero Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreHealth, 0, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreHealth, 5, 0) });
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
        public void ResolveInstantaneousEffects_RestoreFatigue_ZeroMagnitudeLeavesFatigueUnchanged()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 7);
            var spell = new SpellDefinition(
                "restore_fatigue_zero_test",
                "Restore Fatigue Zero Test",
                MagicSchool.Restoration,
                1,
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 0, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreMana, 7, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreMana, 8, 0) });
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
                    new SpellEffectSpec(SpellEffectKind.RestoreMana, 6, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 2, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreHealth, 3, 0),
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
                new[] { new SpellEffectSpec(SpellEffectKind.RestoreMana, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalRestoredMana, Is.EqualTo(0));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(8));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectMana_DrainsTargetMana()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 14, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_mana_test",
                "Direct Mana Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectMana, 5, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.DirectMana, 9, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.DirectMana, 11, 0) });
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
                    new SpellEffectSpec(SpellEffectKind.DirectMana, 6, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreMana, 3, 0),
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
        public void ResolveInstantaneousEffects_DirectMana_ZeroMagnitudeLeavesManaUnchanged()
        {
            var target = CreateActor(601, "Acolyte", ActorRole.Player, health: 16, mana: 8);
            var spell = new SpellDefinition(
                "direct_mana_zero_test",
                "Direct Mana Zero Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectMana, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDirectManaDamage, Is.EqualTo(0));
            Assert.That(target.Vitals.Mana.Current, Is.EqualTo(8));
        }

        [Test]
        public void ResolveInstantaneousEffects_DirectFatigue_DrainsTargetFatigue()
        {
            var target = CreateActor(601, "Runner", ActorRole.Guard, health: 16, mana: 4, fatigue: 12);
            var spell = new SpellDefinition(
                "direct_fatigue_test",
                "Direct Fatigue Test",
                MagicSchool.Destruction,
                1,
                new[] { new SpellEffectSpec(SpellEffectKind.DirectFatigue, 5, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.DirectFatigue, 9, 0) });
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
                new[] { new SpellEffectSpec(SpellEffectKind.DirectFatigue, 11, 0) });
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
                    new SpellEffectSpec(SpellEffectKind.DirectFatigue, 6, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 3, 0),
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
                    new SpellEffectSpec(SpellEffectKind.DirectFatigue, 11, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 5, 0),
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
                new[] { new SpellEffectSpec(SpellEffectKind.DirectFatigue, 0, 0) });
            var cast = SpellCastResult.Ok(spell, 1, "cast");
            var service = new SpellEffectResolutionService();

            var result = service.ResolveInstantaneousEffects(cast, target);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalDirectFatigueDamage, Is.EqualTo(0));
            Assert.That(target.Vitals.Fatigue.Current, Is.EqualTo(8));
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
                    new SpellEffectSpec(SpellEffectKind.DirectDamage, 3, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreHealth, 4, 0),
                    new SpellEffectSpec(SpellEffectKind.DirectFatigue, 4, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreFatigue, 3, 0),
                    new SpellEffectSpec(SpellEffectKind.DirectMana, 5, 0),
                    new SpellEffectSpec(SpellEffectKind.RestoreMana, 6, 0),
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
