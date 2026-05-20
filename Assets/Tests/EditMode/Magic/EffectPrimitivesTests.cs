using EmberCrpg.Domain.Magic;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Magic
{
    public sealed class EffectPrimitivesTests
    {
        [Test]
        public void EffectOperationKind_StableCodes()
        {
            Assert.That(EffectOperationKind.DirectDamage.Code, Is.EqualTo("direct_damage"));
            Assert.That(EffectOperationKind.DirectRestore.Code, Is.EqualTo("direct_restore"));
            Assert.That(EffectOperationKind.StatusApply.Code, Is.EqualTo("status_apply"));
            Assert.That(EffectOperationKind.AreaApply.Code, Is.EqualTo("area_apply"));
            Assert.That(EffectOperationKind.TerrainApply.Code, Is.EqualTo("terrain_apply"));
        }

        [Test]
        public void EffectOperation_RejectsInvalid()
        {
            Assert.Throws<System.ArgumentException>(() => new EffectOperation(default, 0, "self", 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new EffectOperation(EffectOperationKind.DirectDamage, -1, "self", 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new EffectOperation(EffectOperationKind.DirectDamage, 5, "self", -1));
        }

        [Test]
        public void EffectDefinition_HappyPath()
        {
            var def = new EffectDefinition(
                new EffectId(1UL),
                "fire",
                new[] { new EffectOperation(EffectOperationKind.DirectDamage, 10, "target", 5) },
                cost: 5,
                cooldownTicks: 10);
            Assert.That(def.SchoolTag, Is.EqualTo("fire"));
            Assert.That(def.Operations.Count, Is.EqualTo(1));
            Assert.That(def.Cost, Is.EqualTo(5));
            Assert.That(def.CooldownTicks, Is.EqualTo(10));
        }

        [Test]
        public void EffectDefinition_RejectsInvalid()
        {
            var ops = new[] { new EffectOperation(EffectOperationKind.DirectDamage, 5, "self", 0) };
            Assert.Throws<System.ArgumentException>(() => new EffectDefinition(default, "fire", ops, 0, 0));
            Assert.Throws<System.ArgumentException>(() => new EffectDefinition(new EffectId(1UL), "", ops, 0, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new EffectDefinition(new EffectId(1UL), "fire", ops, -1, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new EffectDefinition(new EffectId(1UL), "fire", ops, 0, -1));
        }

        [Test]
        public void EffectRegistry_RegisterAndLookup()
        {
            var registry = new EffectRegistry();
            var def = new EffectDefinition(new EffectId(1UL), "fire", null, 0, 0);
            registry.Register(def);

            Assert.That(registry.Contains(new EffectId(1UL)), Is.True);
            Assert.That(registry.TryGet(new EffectId(1UL), out var found), Is.True);
            Assert.That(found, Is.SameAs(def));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void EffectRegistry_RejectsDuplicate()
        {
            var registry = new EffectRegistry();
            registry.Register(new EffectDefinition(new EffectId(1UL), "fire", null, 0, 0));
            Assert.Throws<System.InvalidOperationException>(() =>
                registry.Register(new EffectDefinition(new EffectId(1UL), "ice", null, 0, 0)));
        }
    }
}
