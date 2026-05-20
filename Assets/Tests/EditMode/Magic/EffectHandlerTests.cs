using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.Process;
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

        // Codex audit Batch 2 / Finding 2 regression:
        // The resolver loop used to mutate `SpellResolverContext` (TargetActor vitals,
        // terrain stockpile) per-operation, only checking afterwards whether any
        // operation lacked a handler. That meant an unhandled tail-op produced a
        // partially-mutated world but reported `Failed`. Fail BEFORE mutating: when
        // any operation has no handler, no state change must escape.
        [Test]
        public void Resolver_UnhandledOperation_LeavesTargetVitalsUntouched()
        {
            var handlers = new EffectOperationHandlers();
            handlers.Register(EffectOperationKind.DirectDamage, op => op.Magnitude);
            // Intentionally do NOT register DirectRestore — second op is unhandled.

            var def = new EffectDefinition(new EffectId(2UL), "halt",
                new[]
                {
                    new EffectOperation(EffectOperationKind.DirectDamage, 4, "target", 0),
                    new EffectOperation(EffectOperationKind.DirectRestore, 3, "self", 0),
                },
                cost: 0, cooldownTicks: 0);

            var target = new ActorRecord(
                new ActorId(7UL),
                "dummy",
                ActorRole.Enemy,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(20, 20), new VitalStat(12, 12), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 0,
                baseDamage: 1);
            var initialHealth = target.Vitals.Health.Current;
            var ctx = new SpellResolverContext(target, terrainStockpile: null, requiredTerrainTag: null, resultTerrainTag: null);
            var events = new WorldEventLog();

            var result = new SpellResolver(handlers).Resolve(def, casterMana: 100, default, new SiteId(1UL), events, ctx);

            Assert.That(result.Resolved, Is.False, "result should report failed when any op has no handler");
            Assert.That(result.FailureReason, Does.StartWith("unhandled_operations"));
            // The pre-validation fix means the handled DirectDamage op must NOT have
            // touched the target's vitals before the loop bailed.
            Assert.That(target.Vitals.Health.Current, Is.EqualTo(initialHealth),
                "target vitals must not be mutated when the spell fails pre-validation");
        }

        [Test]
        public void Resolver_UnhandledOperation_LeavesTerrainStockpileUntouched()
        {
            var handlers = new EffectOperationHandlers();
            handlers.Register(EffectOperationKind.TerrainApply, op => op.Magnitude);
            // DirectDamage left unregistered — second op is unhandled.

            var def = new EffectDefinition(new EffectId(3UL), "scorch_then_burst",
                new[]
                {
                    new EffectOperation(EffectOperationKind.TerrainApply, 1, "fuel_dry", 0),
                    new EffectOperation(EffectOperationKind.DirectDamage, 6, "target", 0),
                },
                cost: 0, cooldownTicks: 0);

            var stockpile = new StockpileComponent(new SiteId(9UL));
            stockpile.Add("fuel_dry", 2);
            var ctx = new SpellResolverContext(targetActor: null, terrainStockpile: stockpile,
                requiredTerrainTag: "fuel_dry", resultTerrainTag: "scorched");
            var events = new WorldEventLog();

            var result = new SpellResolver(handlers).Resolve(def, casterMana: 100, default, new SiteId(9UL), events, ctx);

            Assert.That(result.Resolved, Is.False);
            // Pre-validation must skip the TerrainApply consumption when DirectDamage
            // has no handler. fuel_dry count stays at the initial 2.
            Assert.That(stockpile.Get("fuel_dry"), Is.EqualTo(2),
                "terrain stockpile must not be consumed when the spell fails pre-validation");
            Assert.That(stockpile.Get("scorched"), Is.EqualTo(0),
                "result terrain must not be produced when the spell fails pre-validation");
        }
    }
}
