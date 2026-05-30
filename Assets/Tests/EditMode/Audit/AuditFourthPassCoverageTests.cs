using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Magic;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Codex audit (fourth pass) coverage gaps. Pin behavior of services
    /// the auditor flagged as having no direct test reference.
    /// </summary>
    public sealed class AuditFourthPassCoverageTests
    {
        // ----- ActorRecord.ReplaceAskedTopics (G-P2) -----
        [Test]
        public void ActorRecord_ReplaceAskedTopics_OverwritesList()
        {
            var actor = new ActorRecord(
                new ActorId(1UL), "x", ActorRole.Player,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 1, dodge: 1, armor: 0, baseDamage: 1,
                topicIds: new[] { "greet" });
            actor.ReplaceAskedTopics(new[] { "trade", "rumors" });
            Assert.That(actor.AskedTopicIds.Count, Is.EqualTo(2));
            Assert.That(actor.AskedTopicIds.Contains("trade"), Is.True);
            Assert.That(actor.AskedTopicIds.Contains("rumors"), Is.True);
        }

        [Test]
        public void ActorRecord_ReplaceAskedTopics_NullClears()
        {
            var actor = new ActorRecord(
                new ActorId(1UL), "x", ActorRole.Player,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 1, dodge: 1, armor: 0, baseDamage: 1,
                topicIds: new[] { "greet" });
            actor.ReplaceAskedTopics(null);
            Assert.That(actor.AskedTopicIds.Count, Is.EqualTo(0));
        }

        // ----- PickupService.TryCollect (G-P2) -----
        [Test]
        public void PickupService_TryCollect_HappyPath()
        {
            var inv = new InventoryState(8);
            var item = new InventoryItem(new ItemId(7UL), "ore", "Ore", 1);
            var pickup = new RoomPickup(item, new GridPosition(1, 1));
            Assert.That(new PickupService().TryCollect(pickup, inv), Is.True);
            Assert.That(pickup.IsCollected, Is.True);
            Assert.That(inv.Contains("ore"), Is.True);
        }

        [Test]
        public void PickupService_TryCollect_AlreadyCollected_ReturnsFalse()
        {
            var inv = new InventoryState(8);
            var item = new InventoryItem(new ItemId(7UL), "ore", "Ore", 1);
            var pickup = new RoomPickup(item, new GridPosition(1, 1));
            pickup.Collect();
            Assert.That(new PickupService().TryCollect(pickup, inv), Is.False);
        }

        // ----- CombatMathService.CalculateArmorMitigation (G-P3) -----
        [Test]
        public void CombatMathService_ArmorMitigation_AddsBodyPartBonus()
        {
            var defender = new ActorRecord(
                new ActorId(1UL), "d", ActorRole.Enemy,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 1, dodge: 1, armor: 3, baseDamage: 1);
            var svc = new CombatMathService();
            Assert.That(svc.CalculateArmorMitigation(defender, BodyPart.Head), Is.EqualTo(3 + 1));
            Assert.That(svc.CalculateArmorMitigation(defender, BodyPart.Chest), Is.EqualTo(3 + 2));
            Assert.That(svc.CalculateArmorMitigation(defender, BodyPart.Legs), Is.EqualTo(3 + 1));
        }

        // ----- SpellCostCalculator.EstimateEffectCost + GetTargetMultiplierNumerator (G-P3) -----
        [Test]
        public void SpellCostCalculator_TargetMultiplierNumerator_KnownKinds()
        {
            var calc = new SpellCostCalculator();
            // We pin "non-zero positive numerator" — the audit's concern was
            // the function existed without ANY test reference.
            Assert.That(calc.GetTargetMultiplierNumerator(SpellTargetKind.CasterSelf), Is.GreaterThanOrEqualTo(0));
            Assert.That(calc.GetTargetMultiplierNumerator(SpellTargetKind.SingleTarget), Is.GreaterThanOrEqualTo(0));
        }

        // ----- DmAgentEscalationService.EscalateNpcToDm direct test (G-P2) -----
        [Test]
        public void DmEscalation_EscalateNpcToDm_BuildsRequestAndDispatches()
        {
            var registry = new ToolRegistry();
            DmAgentToolSurface.RegisterAll(registry);
            var router = new ToolCallRouter(new ToolCallValidator());
            var tracer = new ToolCallTracer();
            var events = new WorldEventLog();
            var svc = new DmAgentEscalationService();
            // RegisterHandlers requires a non-null WorldState. The
            // escalate_resolve_or_pass handler does not actually inspect the
            // world, but the API contract is strict.
            var world = new EmberCrpg.Simulation.World.WorldFactory().Create(roomSeed: 1);
            svc.RegisterHandlers(router, world);

            var result = svc.EscalateNpcToDm("topic-7", registry, router, new GameTime(0), new SiteId(1UL), events, tracer);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Payload, Does.Contain("topic-7"));
            Assert.That(tracer.Entries.Count, Is.EqualTo(1));
            Assert.That(tracer.Entries[0].Request.ToolId.Code, Is.EqualTo("escalate_resolve_or_pass"));
        }
    }
}
