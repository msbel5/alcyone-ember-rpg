using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Magic
{
    public sealed class EffectHandlerTests
    {
        [Test]
        public void Handlers_DispatchByKind()
        {
            var h = new EffectOperationHandlers();
            h.Register(EffectOperationKind.DirectDamage, op => op.Magnitude * 2);

            var op = new EffectOperation(EffectOperationKind.DirectDamage, 5, "target", 0);
            Assert.That(h.TryHandle(op, out var applied), Is.True);
            Assert.That(applied, Is.EqualTo(10));
        }

        [Test]
        public void Handlers_UnknownKind_ReturnsFalse()
        {
            var h = new EffectOperationHandlers();
            var op = new EffectOperation(EffectOperationKind.AreaApply, 5, "tile", 0);
            Assert.That(h.TryHandle(op, out _), Is.False);
        }

        [Test]
        public void Resolver_InsufficientMana_Fails()
        {
            var handlers = new EffectOperationHandlers();
            var def = new EffectDefinition(new EffectId(1UL), "fire", null, cost: 10, cooldownTicks: 0);
            var events = new WorldEventLog();
            var result = new SpellResolver(handlers).Resolve(def, casterMana: 5, default, new SiteId(1UL), events);
            Assert.That(result.Resolved, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo("insufficient_mana"));
            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void Resolver_AppliesAllOperations_EmitsSpellResolved()
        {
            var handlers = new EffectOperationHandlers();
            handlers.Register(EffectOperationKind.DirectDamage, op => op.Magnitude);
            handlers.Register(EffectOperationKind.DirectRestore, op => op.Magnitude);

            var def = new EffectDefinition(new EffectId(1UL), "mixed",
                new[]
                {
                    new EffectOperation(EffectOperationKind.DirectDamage, 7, "target", 0),
                    new EffectOperation(EffectOperationKind.DirectRestore, 3, "self", 0),
                },
                cost: 0, cooldownTicks: 0);
            var events = new WorldEventLog();
            var result = new SpellResolver(handlers).Resolve(def, casterMana: 100, default, new SiteId(1UL), events);

            Assert.That(result.Resolved, Is.True);
            Assert.That(result.OperationsApplied, Is.EqualTo(2));
            Assert.That(result.TotalMagnitude, Is.EqualTo(10));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(WorldEventKind.SpellResolved));
        }
    }
}
