using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Magic;
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
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId },
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
        public void TryExecute_OutOfRangeTarget_DoesNotSpendManaOrMutateTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var farEnemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 10, y: 1, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId },
                farEnemy);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.TargetRejected));
            Assert.That(result.TargetValidationResult.Error, Is.EqualTo(SpellTargetValidationError.TargetOutOfRange));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
            Assert.That(farEnemy.Vitals.Health.Current, Is.EqualTo(16));
        }

        [Test]
        public void TryExecute_TimedUnsupportedEffect_DoesNotSpendManaOrMutateCaster()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 20);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                SliceSpellCatalog.EmberWardTemplateId,
                new[] { SliceSpellCatalog.EmberWardTemplateId },
                requestedTarget: caster);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellExecutionError.ResolutionRejected));
            Assert.That(result.EffectResolutionResult.Error, Is.EqualTo(SpellEffectResolutionError.NonInstantaneousEffect));
            Assert.That(result.ManaSpent, Is.EqualTo(0));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(20));
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
                SliceSpellCatalog.MendingTouchTemplateId,
                new[] { SliceSpellCatalog.MendingTouchTemplateId },
                ally);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalHealing, Is.EqualTo(6));
            Assert.That(result.ManaSpent, Is.EqualTo(10));
            Assert.That(caster.Vitals.Mana.Current, Is.EqualTo(10));
            Assert.That(ally.Vitals.Health.Current, Is.EqualTo(15));
        }

        [Test]
        public void TryExecute_InsufficientMana_DoesNotSpendManaOrMutateTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 16, mana: 5);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4, health: 16, mana: 0);
            var service = new SpellExecutionService();

            var result = service.TryExecute(
                caster,
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.FlameBoltTemplateId },
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
                SliceSpellCatalog.FlameBoltTemplateId,
                new[] { SliceSpellCatalog.MendingTouchTemplateId },
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
    }
}
