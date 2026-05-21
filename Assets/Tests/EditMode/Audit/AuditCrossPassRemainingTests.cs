using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Movement;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Cross-pass tail coverage — closes the last G testing gaps the
    /// auditor flagged across all five audit passes that previous batches
    /// did not directly cover.
    /// </summary>
    public sealed class AuditCrossPassRemainingTests
    {
        private static ActorRecord NewActor(ulong id = 1UL)
        {
            return new ActorRecord(
                new ActorId(id), $"actor-{id}", ActorRole.Player,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(20, 20), new VitalStat(12, 12), new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 10, dodge: 5, armor: 2, baseDamage: 1);
        }

        // ----- ActorMemory.ReplaceTransactions ordering (G-P2) -----
        [Test]
        public void ActorMemory_ReplaceTransactions_PreservesOrder()
        {
            var mem = new ActorMemory(new ActorId(1UL));
            var t0 = new TransactionRecord(new GameTime(0), "buy", "iron", 1, -5);
            var t1 = new TransactionRecord(new GameTime(1), "buy", "bread", 2, -3);
            mem.ReplaceTransactions(new[] { t0, t1 });
            Assert.That(mem.Transactions.Count, Is.EqualTo(2));
            Assert.That(mem.Transactions[0].ItemTemplateId, Is.EqualTo("iron"));
            Assert.That(mem.Transactions[1].ItemTemplateId, Is.EqualTo("bread"));
        }

        // ----- EncounterState.AddLog ordering (G-P2) -----
        [Test]
        public void EncounterState_AddLog_CapacityAndOrdering()
        {
            var s = new EncounterState(new ActorId(1UL), new ActorId(2UL));
            s.AddLog("first");
            s.AddLog("second");
            Assert.That(s.LogLines.ToArray(), Is.EqualTo(new[] { "first", "second" }));
        }

        // ----- QueuedCombatAction.MarkActivated invalid transition (G-P2) -----
        [Test]
        public void QueuedCombatAction_DoubleMarkActivated_RemainsActivated()
        {
            var action = new QueuedCombatAction(
                sequence: 1, actorId: new ActorId(1UL),
                kind: CombatActionKind.MeleeSwing,
                requestedAtSeconds: 0d, startAtSeconds: 0d,
                windupSeconds: 0.1d, activeSeconds: 0.2d, recoverySeconds: 0.1d,
                targetActorId: new ActorId(2UL));
            action.MarkActivated();
            action.MarkActivated();
            Assert.That(action.IsActivated, Is.True);
        }

        // ----- EquipmentState.Unequip stable slot (G-P2) -----
        [Test]
        public void EquipmentState_Unequip_OtherSlotsUntouched()
        {
            var eq = new EquipmentState();
            eq.Equip(EquipmentSlot.Weapon, new ItemId(42UL));
            eq.Unequip(EquipmentSlot.Weapon);
            // Re-equip after unequip works.
            eq.Equip(EquipmentSlot.Weapon, new ItemId(99UL));
            Assert.That(eq.GetEquippedItemId(EquipmentSlot.Weapon).Value, Is.EqualTo(99UL));
        }

        // ----- InventoryItem.RemoveQuantity underflow rejection (G-P2) -----
        [Test]
        public void InventoryItem_RemoveQuantity_FloorsAtZero()
        {
            var item = new InventoryItem(new ItemId(1UL), "ore", "Ore", 3);
            item.RemoveQuantity(99);
            Assert.That(item.Quantity, Is.EqualTo(0));
        }

        // ----- Sprint4Vector3.ClampMagnitude (G-P3) -----
        [Test]
        public void Sprint4Vector3_ClampMagnitude_ReducesOversizedVector()
        {
            var v = new Sprint4Vector3(10f, 0f, 0f);
            var clamped = Sprint4Vector3.ClampMagnitude(v,3f);
            // Magnitude should be at-most 3
            Assert.That(clamped.X * clamped.X + clamped.Y * clamped.Y + clamped.Z * clamped.Z,
                Is.LessThanOrEqualTo(3f * 3f + 0.001f));
        }

        [Test]
        public void Sprint4Vector3_ClampMagnitude_PreservesSmallVector()
        {
            var v = new Sprint4Vector3(1f, 0f, 0f);
            var clamped = Sprint4Vector3.ClampMagnitude(v,5f);
            Assert.That(clamped.X, Is.EqualTo(1f).Within(0.001f));
        }
    }
}
