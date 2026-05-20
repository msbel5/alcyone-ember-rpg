using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

// Design note:
// Pins the Sprint 5 spell target validator: deterministic refusal/routing for CasterSelf,
// Touch (orthogonal adjacency), SingleTarget range enforcement, and explicit refusal of area kinds
// until area resolution lands.
namespace EmberCrpg.Tests.EditMode.Magic
{
    /// <summary>Verifies deterministic spell target validation and routing.</summary>
    public sealed class SpellTargetValidatorTests
    {
        [Test]
        public void Validate_CasterSelfWithNullTarget_RoutesToCaster()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateEmberWard(), caster, requestedTarget: null);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.None));
            Assert.That(result.RoutedTarget, Is.SameAs(caster));
        }

        [Test]
        public void Validate_CasterSelfWithCasterAsTarget_RoutesToCaster()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateEmberWard(), caster, caster);

            Assert.That(result.Success, Is.True);
            Assert.That(result.RoutedTarget, Is.SameAs(caster));
        }

        [Test]
        public void Validate_CasterSelfWithOtherTarget_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var other = CreateActor(702, "Guard", ActorRole.Guard, x: 1, y: 2);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateEmberWard(), caster, other);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.WrongTargetForSelfSpell));
            Assert.That(result.RoutedTarget, Is.Null);
        }

        [Test]
        public void Validate_TouchWithOrthogonallyAdjacentTarget_RoutesToTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var ally = CreateActor(702, "Guard", ActorRole.Guard, x: 1, y: 2);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, ally);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.None));
            Assert.That(result.RoutedTarget, Is.SameAs(ally));
        }

        [Test]
        public void Validate_TouchWithDiagonalTarget_IsRefusedAsNotAdjacent()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var ally = CreateActor(702, "Guard", ActorRole.Guard, x: 2, y: 2);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, ally);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.TargetNotAdjacent));
        }

        [Test]
        public void Validate_TouchWithSelfTile_IsRefusedAsNotAdjacent()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, caster);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.TargetNotAdjacent));
        }

        [Test]
        public void Validate_TouchWithFarTarget_IsRefusedAsNotAdjacent()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var ally = CreateActor(702, "Guard", ActorRole.Guard, x: 4, y: 1);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, ally);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.TargetNotAdjacent));
        }

        [Test]
        public void Validate_TouchWithNullTarget_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, requestedTarget: null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.InvalidTarget));
        }

        [Test]
        public void Validate_TouchWithDeadTarget_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var corpse = CreateActor(702, "Fallen", ActorRole.Guard, x: 1, y: 2, health: 0);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, corpse);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.InvalidTarget));
        }

        [Test]
        public void Validate_SingleTargetWithLivingTargetInsideConfiguredRange_RoutesToTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var enemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 5, y: 4);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateFlameBolt(), caster, enemy);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.None));
            Assert.That(result.RoutedTarget, Is.SameAs(enemy));
        }

        [Test]
        public void Validate_SingleTargetBeyondConfiguredRange_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var farEnemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 7, y: 4);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateFlameBolt(), caster, farEnemy);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.TargetOutOfRange));
            Assert.That(result.RoutedTarget, Is.Null);
        }

        [Test]
        public void Validate_SingleTargetWithUnboundedRangeZero_AllowsFarTarget()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var farEnemy = CreateActor(801, "Ash Rat", ActorRole.Enemy, x: 20, y: 20);
            var spell = new SpellDefinition(
                "far_sight_test",
                "Far Sight Test",
                MagicSchool.Destruction,
                SpellTargetKind.SingleTarget,
                9,
                0,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 4, 0) });
            var validator = new SpellTargetValidator();

            var result = validator.Validate(spell, caster, farEnemy);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.None));
            Assert.That(result.RoutedTarget, Is.SameAs(farEnemy));
        }

        [Test]
        public void Validate_SingleTargetWithNullTarget_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateFlameBolt(), caster, requestedTarget: null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.InvalidTarget));
        }

        [Test]
        public void Validate_SingleTargetWithDeadTarget_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var corpse = CreateActor(802, "Husk", ActorRole.Enemy, x: 2, y: 1, health: 0);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateFlameBolt(), caster, corpse);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.InvalidTarget));
        }

        [Test]
        public void Validate_AreaAroundCaster_IsRefusedUntilAreaLands()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var spell = new SpellDefinition(
                "burst_test",
                "Burst Test",
                MagicSchool.Destruction,
                SpellTargetKind.AreaAroundCaster,
                10,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 4, 0) });
            var validator = new SpellTargetValidator();

            var result = validator.Validate(spell, caster, requestedTarget: null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.UnsupportedTargetKind));
            Assert.That(result.RoutedTarget, Is.Null);
        }

        [Test]
        public void Validate_AreaAtRange_IsRefusedUntilAreaLands()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var spell = new SpellDefinition(
                "ranged_blast_test",
                "Ranged Blast Test",
                MagicSchool.Destruction,
                SpellTargetKind.AreaAtRange,
                12,
                new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 5, 0) });
            var validator = new SpellTargetValidator();

            var result = validator.Validate(spell, caster, requestedTarget: null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.UnsupportedTargetKind));
            Assert.That(result.RoutedTarget, Is.Null);
        }

        [Test]
        public void Validate_NullSpell_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(spell: null, caster: caster, requestedTarget: null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.InvalidSpell));
        }

        [Test]
        public void Validate_NullCaster_IsRefused()
        {
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateFlameBolt(), caster: null, requestedTarget: null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.InvalidCaster));
        }

        [Test]
        public void Validate_DeadCaster_IsRefused()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1, health: 0);
            var ally = CreateActor(702, "Guard", ActorRole.Guard, x: 1, y: 2);
            var validator = new SpellTargetValidator();

            var result = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, ally);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellTargetValidationError.InvalidCaster));
        }

        [Test]
        public void Validate_RoutedTarget_FlowsCleanlyIntoEffectResolver()
        {
            var caster = CreateActor(701, "Acolyte", ActorRole.Player, x: 1, y: 1);
            var ally = CreateActor(702, "Guard", ActorRole.Guard, x: 1, y: 2, health: 13);
            var validator = new SpellTargetValidator();
            var routed = validator.Validate(SliceSpellCatalog.CreateMendingTouch(), caster, ally);
            Assert.That(routed.Success, Is.True);

            var cast = SpellCastResult.Ok(routed.Spell, 10, "cast");
            var effectResult = new SpellEffectResolutionService().ResolveInstantaneousEffects(cast, routed.RoutedTarget);

            Assert.That(effectResult.Success, Is.True);
            Assert.That(effectResult.TotalHealing, Is.EqualTo(3));
            Assert.That(ally.Vitals.Health.Current, Is.EqualTo(16));
        }

        private static ActorRecord CreateActor(int id, string name, ActorRole role, int x, int y, int health = 16)
        {
            return new ActorRecord(
                new ActorId((ulong)id),
                name,
                role,
                new EmberStatBlock(10, 11, 12, 14, 9, 8),
                new ActorVitals(new VitalStat(health, 16), new VitalStat(12, 12), new VitalStat(20, 20)),
                new GridPosition(x, y),
                accuracy: 11,
                dodge: 6,
                armor: 1,
                baseDamage: 3);
        }
    }
}
