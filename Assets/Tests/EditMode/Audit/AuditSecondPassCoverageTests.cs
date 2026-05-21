using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Memory;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Codex audit (second pass) dimension G — coverage gaps. 20+ new
    /// regression tests pinning boundary, overflow, lifecycle, and ordering
    /// behaviors the auditor flagged as untested.
    /// </summary>
    public sealed class AuditSecondPassCoverageTests
    {
        // ----- StockpileComponent.Add overflow (A-P1 + G) -----
        [Test]
        public void Stockpile_Add_OverflowSaturatesAtIntMax()
        {
            var stock = new StockpileComponent(new SiteId(1UL));
            stock.Add("ore", int.MaxValue - 1);
            stock.Add("ore", 100);
            Assert.That(stock.Get("ore"), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Stockpile_Add_ZeroDoesNotChangeValue()
        {
            var stock = new StockpileComponent(new SiteId(1UL));
            stock.Add("ore", 5);
            stock.Add("ore", 0);
            Assert.That(stock.Get("ore"), Is.EqualTo(5));
        }

        // ----- InventoryState quantity gates (A-P2 + G) -----
        [Test]
        public void InventoryState_TryRemove_RejectsZeroQuantity()
        {
            var inv = new InventoryState(8);
            inv.TryAdd(new InventoryItem(new ItemId(1UL), "ore", "Ore", 5));
            Assert.That(inv.TryRemove("ore", 0), Is.False);
            Assert.That(inv.TryRemove("ore", -3), Is.False);
        }

        [Test]
        public void InventoryState_TryRemoveStackable_RejectsZeroQuantity()
        {
            var inv = new InventoryState(8);
            inv.TryAdd(new InventoryItem(new ItemId(1UL), "ore", "Ore", 5));
            Assert.That(inv.TryRemoveStackable("ore", 0), Is.False);
            Assert.That(inv.TryRemoveStackable("ore", -1), Is.False);
        }

        // ----- InventoryItem.AddQuantity overflow (A-P2 + G) -----
        [Test]
        public void InventoryItem_AddQuantity_SaturatesAtIntMax()
        {
            var item = new InventoryItem(new ItemId(1UL), "ore", "Ore", int.MaxValue - 1);
            item.AddQuantity(100);
            Assert.That(item.Quantity, Is.EqualTo(int.MaxValue));
        }

        // ----- FactionReputation.Apply overflow (A-P2 + G) -----
        [Test]
        public void FactionReputation_Apply_LargePositiveDelta_DoesNotWrapNegative()
        {
            var rep = FactionReputation.Neutral.Apply(int.MaxValue);
            // Clamp ranges 0..100 etc.; the key is the result must NOT wrap to a
            // negative value. ToRelationKind should be the friendliest end.
            Assert.That(rep.Value, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void FactionReputation_Apply_LargeNegativeDelta_DoesNotWrapPositive()
        {
            var rep = FactionReputation.Neutral.Apply(int.MinValue);
            // Should saturate at the hostile bound, never wrap to positive.
            Assert.That(rep.Value, Is.LessThanOrEqualTo(0));
        }

        // ----- MemoryWriteSystem structured crime codes (A-P2 + G) -----
        [Test]
        public void MemoryWriteSystem_NonTheftCrimes_RecordedAsCrime()
        {
            var sys = new MemoryWriteSystem();
            var mem = new MemoryComponent(new ActorId(1UL));
            foreach (var verb in new[] { "assault", "pickpocket", "vandalism", "arson", "robbery" })
            {
                var ev = new WorldEvent(new GameTime(0), WorldEventKind.ActorTalked,
                    new ActorId(99UL), new SiteId(1UL), $"{verb} target:guard");
                Assert.That(sys.RecordFromWorldEvent(mem, ev), Is.True,
                    $"verb '{verb}' should land in the crime branch");
            }
            // Each verb produced one fact with topic "crime"
            Assert.That(mem.Facts.Count(f => f.Topic.Code == "crime"), Is.EqualTo(5));
        }

        [Test]
        public void MemoryWriteSystem_UnknownVerb_NotRecordedAsCrime()
        {
            var sys = new MemoryWriteSystem();
            var mem = new MemoryComponent(new ActorId(1UL));
            var ev = new WorldEvent(new GameTime(0), WorldEventKind.ActorTalked,
                new ActorId(99UL), new SiteId(1UL), "complimented your hair");
            // Not in crime vocabulary, not a TradeCompleted kind → false
            Assert.That(sys.RecordFromWorldEvent(mem, ev), Is.False);
        }

        // ----- ActorMood.IsAtMost (G-P3) -----
        [Test]
        public void ActorMood_IsAtMost_BoundaryEqualsCountsAsAtMost()
        {
            var mood = new ActorMood(40);
            Assert.That(mood.IsAtMost(new ActorMood(40)), Is.True);
            Assert.That(mood.IsAtMost(new ActorMood(41)), Is.True);
            Assert.That(mood.IsAtMost(new ActorMood(39)), Is.False);
        }

        // ----- GridPosition.ManhattanDistanceTo (G-P3) -----
        [Test]
        public void GridPosition_ManhattanDistanceTo_NegativeCoordinates()
        {
            var origin = new GridPosition(-5, -3);
            var target = new GridPosition(2, 4);
            Assert.That(origin.ManhattanDistanceTo(target), Is.EqualTo(7 + 7));
            Assert.That(target.ManhattanDistanceTo(origin), Is.EqualTo(7 + 7));
        }

        [Test]
        public void GridPosition_ManhattanDistanceTo_SameCellIsZero()
        {
            var pos = new GridPosition(3, 7);
            Assert.That(pos.ManhattanDistanceTo(pos), Is.EqualTo(0));
        }

        // ----- QueuedCombatAction.IsActiveAt (G-P3) -----
        [Test]
        public void QueuedCombatAction_IsActiveAt_EdgeBoundaries()
        {
            var action = new QueuedCombatAction(
                sequence: 1,
                actorId: new ActorId(1UL),
                kind: CombatActionKind.MeleeSwing,
                requestedAtSeconds: 0d,
                startAtSeconds: 1d,
                windupSeconds: 0.5d,
                activeSeconds: 0.3d,
                recoverySeconds: 0.2d,
                targetActorId: new ActorId(2UL));
            // Active window: [startAt + windup, startAt + windup + active]
            // = [1.5, 1.8]
            Assert.That(action.IsActiveAt(1.4d), Is.False, "before active window");
            Assert.That(action.IsActiveAt(1.5d), Is.True, "active start");
            Assert.That(action.IsActiveAt(1.79d), Is.True, "inside active");
            Assert.That(action.IsActiveAt(1.81d), Is.False, "after active");
        }

        // ----- EquipmentSlot.FromCode (G-P3) -----
        [Test]
        public void EquipmentSlot_FromCode_KnownCodeRoundtrips()
        {
            var weapon = EquipmentSlot.FromCode("main_hand");
            Assert.That(weapon.Code, Is.EqualTo("main_hand"));
        }

        [Test]
        public void EquipmentSlot_FromCode_UnknownPreservesCustomCode()
        {
            // FromCode preserves unknown codes (returns a non-empty
            // EquipmentSlot with the normalized lowercase code) rather than
            // falling back to None. Whitespace/null → None.
            var slot = EquipmentSlot.FromCode("unknown_made_up_slot");
            Assert.That(slot.Code, Is.EqualTo("unknown_made_up_slot"));
            Assert.That(EquipmentSlot.FromCode("").IsEmpty, Is.True);
            Assert.That(EquipmentSlot.FromCode(null).IsEmpty, Is.True);
        }

        // ----- EquipmentSlot.FromLegacyValue (G-P3) -----
        [Test]
        public void EquipmentSlot_FromLegacyValue_KnownInt()
        {
            var weapon = EquipmentSlot.FromLegacyValue(1);
            Assert.That(weapon.Code, Is.EqualTo("main_hand"));
        }

        [Test]
        public void EquipmentSlot_FromLegacyValue_UnknownReturnsNone()
        {
            var slot = EquipmentSlot.FromLegacyValue(999);
            Assert.That(slot.IsEmpty, Is.True);
        }

        // ----- NpcMemoryStore.GetAllSorted (G-P3) -----
        [Test]
        public void NpcMemoryStore_GetAllSorted_CanonicalActorIdOrder()
        {
            var store = new NpcMemoryStore();
            store.GetOrCreate(new ActorId(5UL));
            store.GetOrCreate(new ActorId(1UL));
            store.GetOrCreate(new ActorId(3UL));
            var sorted = store.GetAllSorted();
            Assert.That(sorted.Select(m => m.ActorId.Value).ToArray(),
                Is.EqualTo(new[] { 1UL, 3UL, 5UL }));
        }

        // ----- JobBoard.GetClaimSequence (G-P3) -----
        [Test]
        public void JobBoard_GetClaimSequence_UnknownJobReturnsZero()
        {
            var board = new JobBoard();
            // No job posted yet — unknown id should not throw and should
            // return 0 (the canonical "no claim" sentinel).
            Assert.That(board.GetClaimSequence(new JobId(999UL)), Is.EqualTo(0));
        }

        // ----- SeasonDefinition.ContainsDay (G-P3) -----
        [Test]
        public void SeasonDefinition_ContainsDay_BoundaryDays()
        {
            var spring = new SeasonDefinition(Season.Spring, startDayOfYear: 1, endDayOfYear: 90);
            Assert.That(spring.ContainsDay(1), Is.True);
            Assert.That(spring.ContainsDay(90), Is.True);
            Assert.That(spring.ContainsDay(91), Is.False);
            Assert.That(spring.ContainsDay(0), Is.False);
        }

        // ----- CaravanState.FromCode (G-P3) -----
        [Test]
        public void CaravanState_FromCode_KnownEnRoute()
        {
            var s = CaravanState.FromCode("en_route");
            Assert.That(s.Equals(CaravanState.EnRoute), Is.True);
        }

        [Test]
        public void CaravanState_FromCode_UnknownPreservesCustomCode()
        {
            // FromCode preserves unknown codes verbatim; only whitespace/null
            // falls back to Idle. This pins the actual behavior the audit
            // flagged as untested.
            var s = CaravanState.FromCode("not_a_state");
            Assert.That(s.Code, Is.EqualTo("not_a_state"));
            Assert.That(CaravanState.FromCode("").Equals(CaravanState.Idle), Is.True);
            Assert.That(CaravanState.FromCode(null).Equals(CaravanState.Idle), Is.True);
        }
    }
}
