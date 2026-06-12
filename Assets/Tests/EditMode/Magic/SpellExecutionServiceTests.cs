using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 spell execution pipeline: cast prechecks, target routing, resolution support,
// and atomic mana/target mutation semantics across the end-to-end flow.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic end-to-end spell execution.</summary>
    public sealed class SpellExecutionServiceTests
    {
        [Test]
        public void TryExecute_InRangeDamageSpell_SpendsManaAndDamagesTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                enemy);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.None));
            Assert.That(result.ManaSpent, Is.EqualTo(12));
            Assert.That(result.TotalDamage, Is.EqualTo(8));
            Assert.That(result.RoutedTarget, Is.SameAs(enemy));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(8));
            Assert.That(enemy.Vitals.Health.Current, Is.EqualTo(8));
        }

        [Test]
        public void TryExecute_CatalogFlameBolt_WithCooldownState_StartsCatalogCooldownAndRejectsRecast()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var cooldownState = new SpellCooldownState();
            var service = new SpellExecutionService();

            var first = service.TryExecute(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                enemy,
                cooldownState);

            Assert.That(first.Success, Is.True);
            Assert.That(first.Error, Is.EqualTo(SpellExecutionError.None));
            Assert.That(cooldownState.GetRemainingTicks(WorldSpellCatalog.FlameBoltTemplateId),
                Is.EqualTo(WorldSpellCatalog.FlameBoltCooldownTicks));

            var second = service.TryExecute(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                enemy,
                cooldownState);

            Assert.That(second.Success, Is.False);
            Assert.That(second.Error, Is.EqualTo(SpellExecutionError.CastRejected));
            Assert.That(second.CastResult.Error, Is.EqualTo(SpellCastError.SpellOnCooldown));
            Assert.That(second.ManaSpent, Is.EqualTo(0));
            Assert.That(cooldownState.GetRemainingTicks(WorldSpellCatalog.FlameBoltTemplateId),
                Is.EqualTo(WorldSpellCatalog.FlameBoltCooldownTicks));
        }

        [Test]
        public void TryExecute_CooldownSpellSuccess_StartsCooldownAfterCommittedCast()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var cooldownState = new SpellCooldownState();
            var cooldownSpell = CreateCooldownSpell();
            var service = CreateCooldownExecutionService(cooldownSpell);

            var result = service.TryExecute(
                caster,
                cooldownSpell.TemplateId,
                new[] { cooldownSpell.TemplateId },
                enemy,
                cooldownState);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.None));
            Assert.That(result.ManaSpent, Is.EqualTo(12));
            Assert.That(result.TotalDamage, Is.EqualTo(8));
            Assert.That(cooldownState.GetRemainingTicks(cooldownSpell.TemplateId), Is.EqualTo(cooldownSpell.CooldownTicks));
        }

        [Test]
        public void TryExecute_ActiveCooldown_RejectsBeforeTargetValidationOrManaSpend()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var cooldownState = new SpellCooldownState();
            var cooldownSpell = CreateCooldownSpell();
            cooldownState.SetRemainingTicks(cooldownSpell.TemplateId, 3);
            var service = CreateCooldownExecutionService(cooldownSpell);

            var result = service.TryExecute(
                caster,
                cooldownSpell.TemplateId,
                new[] { cooldownSpell.TemplateId },
                enemy,
                cooldownState);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.CastRejected));
            Assert.That(result.CastResult.Error, Is.EqualTo(SpellCastError.SpellOnCooldown));
            Assert.That(result.TargetValidationResult, Is.Null);
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(cooldownState.GetRemainingTicks(cooldownSpell.TemplateId), Is.EqualTo(3));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(enemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecute_TargetRejected_DoesNotStartCooldown()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var farEnemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 20, y: 1, health: 16, mana: 0);
            var cooldownState = new SpellCooldownState();
            var cooldownSpell = CreateCooldownSpell();
            var service = CreateCooldownExecutionService(cooldownSpell);

            var result = service.TryExecute(
                caster,
                cooldownSpell.TemplateId,
                new[] { cooldownSpell.TemplateId },
                farEnemy,
                cooldownState);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.TargetRejected));
            Assert.That(result.TargetValidationResult.Error, Is.EqualTo(SpellTargetValidationError.TargetOutOfRange));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(cooldownState.GetRemainingTicks(cooldownSpell.TemplateId), Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(farEnemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecuteWithRoll_RollFizzle_DoesNotSpendManaMutateTargetOrStartCooldown()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var cooldownState = new SpellCooldownState();
            var cooldownSpell = CreateCooldownSpell();
            var service = CreateCooldownExecutionService(cooldownSpell);

            var result = service.TryExecuteWithRoll(
                caster,
                cooldownSpell.TemplateId,
                new[] { cooldownSpell.TemplateId },
                enemy,
                new XorShiftRng(1u),
                cooldownState);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.CastFizzled));
            Assert.That(result.Rolled, Is.True);
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(cooldownState.GetRemainingTicks(cooldownSpell.TemplateId), Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(enemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecute_OutOfRangeTarget_DoesNotSpendManaOrMutateTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var farEnemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 10, y: 1, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                farEnemy);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.TargetRejected));
            Assert.That(result.TargetValidationResult.Error, Is.EqualTo(SpellTargetValidationError.TargetOutOfRange));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(farEnemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecute_TimedEffectSpell_CommitsManaAndSkipsTimedResolution()
        {
            // F28 contract change: ember_ward's timed shield no longer aborts the execution —
            // the cast commits (mana + cooldown), the instantaneous resolver skips the timed
            // row (0 applied), and the buff systems own the shield. The old all-or-nothing
            // rejection silently refused ember_ward in live play.
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                WorldSpellCatalog.EmberWardTemplateId,
                new[] { WorldSpellCatalog.EmberWardTemplateId },
                requestedTarget: caster);

            Assert.That(result.Success, Is.True);
            Assert.That(result.EffectResolutionResult.Success, Is.True);
            Assert.That(result.EffectResolutionResult.AppliedEffectCount, Is.EqualTo(0));
            Assert.That(result.ManaSpent, Is.EqualTo(15));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(5));
            Assert.That(caster.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecute_AdjacentHealingSpell_SpendsManaAndRestoresHealth()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var ally = CreateActor(702, "Guard", ActorRole.Guard, x: 1, y: 2, health: 9, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                WorldSpellCatalog.MendingTouchTemplateId,
                new[] { WorldSpellCatalog.MendingTouchTemplateId },
                ally);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalHealing, Is.EqualTo(6));
            Assert.That(result.ManaSpent, Is.EqualTo(10));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(10));
            Assert.That(ally.Vitals.Health.Current, Is.EqualTo(15));
        }

        [Test]
        public void TryExecuteWithRoll_RollSuccess_SpendsManaAndDamagesTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecuteWithRoll(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                enemy,
                new XorShiftRng(3u));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.None));
            Assert.That(result.Rolled, Is.True);
            Assert.That(result.RollValue, Is.EqualTo(8));
            Assert.That(result.RollThreshold, Is.EqualTo(29));
            Assert.That(result.ManaSpent, Is.EqualTo(12));
            Assert.That(result.TotalDamage, Is.EqualTo(8));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(8));
            Assert.That(enemy.Vitals.Health.Current, Is.EqualTo(8));
        }

        [Test]
        public void TryExecuteWithRoll_RollFizzle_DoesNotSpendManaOrMutateTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecuteWithRoll(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                enemy,
                new XorShiftRng(1u));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.CastFizzled));
            Assert.That(result.Rolled, Is.True);
            Assert.That(result.RollValue, Is.EqualTo(70));
            Assert.That(result.RollThreshold, Is.EqualTo(29));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(enemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecuteWithRoll_PrecheckRefusal_HappensBeforeAnyRollOrManaSpend()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var farEnemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 10, y: 1, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecuteWithRoll(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                farEnemy,
                new XorShiftRng(3u));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.TargetRejected));
            Assert.That(result.CastRollResult, Is.Null);
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(farEnemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecute_InsufficientMana_DoesNotSpendManaOrMutateTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 5);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.FlameBoltTemplateId },
                enemy);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.CastRejected));
            Assert.That(result.CastResult.Error, Is.EqualTo(SpellCastError.InsufficientMana));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(5));
            Assert.That(enemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecute_UnlearnedSpell_DoesNotSpendMana()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                WorldSpellCatalog.FlameBoltTemplateId,
                new[] { WorldSpellCatalog.MendingTouchTemplateId },
                enemy);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.CastRejected));
            Assert.That(result.CastResult.Error, Is.EqualTo(SpellCastError.SpellNotKnown));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(enemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        private static ActorRecord CreateActor(int id, string name, ActorRole role, int x, int y, int health, int mana)
        {
            return new ActorRecord(
                new ActorId((ulong)id),
                name,
                role,
                new EmberStatBlock(10, 11, 12, 14, 9, 8),
                new ActorVitals(new VitalStat(health, 16), new VitalStat(12, 12), new VitalStat(mana, 20)),
                new GridPosition(x, y),
                accuracy: 11,
                dodge: 6,
                armor: 1,
                baseDamage: 3);
        }

        private static SpellDefinition CreateCooldownSpell()
        {
            return new SpellDefinition(
                "cooldown_flame_bolt",
                "Cooldown Flame Bolt",
                MagicSchool.Destruction,
                SpellTargetKind.SingleTarget,
                manaCost: 12,
                rangeInTiles: 8,
                cooldownTicks: 5,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 8, 0) });
        }

        private static SpellExecutionService CreateCooldownExecutionService(SpellDefinition cooldownSpell)
        {
            return new SpellExecutionService(
                new SpellCastingService(templateId => templateId == cooldownSpell.TemplateId ? cooldownSpell : null),
                new SpellTargetValidator(),
                new SpellEffectResolutionService(),
                new SpellCastRollService());
        }
    }
}
