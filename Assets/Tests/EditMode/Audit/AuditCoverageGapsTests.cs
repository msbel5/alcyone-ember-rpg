using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Codex audit dimension G — coverage gaps. The auditor flagged 23 mutators
    /// and value-transforms that lacked any roundtrip / boundary / lifecycle
    /// test. These regression tests pin the current contract so future
    /// refactors cannot quietly change semantics.
    /// </summary>
    public sealed class AuditCoverageGapsTests
    {
        private static ActorRecord NewActor(ulong id = 1UL, string name = "tester", ActorRole role = ActorRole.Player)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(20, 20), new VitalStat(12, 12), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 0,
                baseDamage: 1);
        }

        // ----- ActorRecord.ApplyMemory -----
        [Test]
        public void ActorRecord_ApplyMemory_RejectsNullMemory()
        {
            var actor = NewActor();
            Assert.Throws<System.ArgumentNullException>(() => actor.ApplyMemory(null));
        }

        [Test]
        public void ActorRecord_ApplyMemory_RejectsForeignOwner()
        {
            var actor = NewActor(id: 7UL);
            var foreignMemory = new MemoryComponent(new ActorId(99UL));
            Assert.Throws<System.ArgumentException>(() => actor.ApplyMemory(foreignMemory));
        }

        [Test]
        public void ActorRecord_ApplyMemory_AssignsMatchingOwner()
        {
            var actor = NewActor(id: 7UL);
            var mem = new MemoryComponent(new ActorId(7UL));
            actor.ApplyMemory(mem);
            Assert.That(actor.Memory, Is.SameAs(mem));
        }

        // ----- ActorNeeds.WithHunger/Fatigue/Thirst -----
        [Test]
        public void ActorNeeds_WithHunger_ClampsAboveMax()
        {
            var needs = new ActorNeeds(new NeedValue(0), new NeedValue(0), new NeedValue(0));
            var raised = needs.WithHunger(new NeedValue(999));
            Assert.That(raised.Hunger.Value, Is.EqualTo(NeedValue.Max));
        }

        [Test]
        public void ActorNeeds_WithFatigue_ClampsBelowZero()
        {
            var needs = new ActorNeeds(new NeedValue(50), new NeedValue(50), new NeedValue(50));
            var rested = needs.WithFatigue(new NeedValue(-10));
            Assert.That(rested.Fatigue.Value, Is.EqualTo(0));
        }

        [Test]
        public void ActorNeeds_WithThirst_DoesNotTouchOtherNeeds()
        {
            var needs = new ActorNeeds(new NeedValue(20), new NeedValue(30), new NeedValue(40));
            var thirsty = needs.WithThirst(new NeedValue(80));
            Assert.That(thirsty.Hunger.Value, Is.EqualTo(20));
            Assert.That(thirsty.Fatigue.Value, Is.EqualTo(30));
            Assert.That(thirsty.Thirst.Value, Is.EqualTo(80));
        }

        // ----- ActorVitals.WithFatigue/WithMana -----
        [Test]
        public void ActorVitals_WithFatigue_ReplacesOnlyFatigue()
        {
            var vitals = new ActorVitals(new VitalStat(20, 20), new VitalStat(10, 12), new VitalStat(8, 10));
            var next = vitals.WithFatigue(new VitalStat(5, 12));
            Assert.That(next.Health.Current, Is.EqualTo(20));
            Assert.That(next.Fatigue.Current, Is.EqualTo(5));
            Assert.That(next.Mana.Current, Is.EqualTo(8));
        }

        [Test]
        public void ActorVitals_WithMana_ReplacesOnlyMana()
        {
            var vitals = new ActorVitals(new VitalStat(20, 20), new VitalStat(10, 12), new VitalStat(8, 10));
            var next = vitals.WithMana(new VitalStat(2, 10));
            Assert.That(next.Health.Current, Is.EqualTo(20));
            Assert.That(next.Fatigue.Current, Is.EqualTo(10));
            Assert.That(next.Mana.Current, Is.EqualTo(2));
        }

        // ----- EncounterState.AddLog -----
        [Test]
        public void EncounterState_AddLog_PreservesInsertionOrder()
        {
            var s = new EncounterState(new ActorId(1UL), new ActorId(2UL));
            s.AddLog("first");
            s.AddLog("second");
            s.AddLog("third");
            Assert.That(s.LogLines.ToArray(), Is.EqualTo(new[] { "first", "second", "third" }));
        }

        // ----- QueuedCombatAction.MarkActivated / MarkCompleted -----
        [Test]
        public void QueuedCombatAction_LifecycleFlags_StartFalseAndTransition()
        {
            var action = new QueuedCombatAction(
                sequence: 1,
                actorId: new ActorId(1UL),
                kind: CombatActionKind.MeleeSwing,
                requestedAtSeconds: 0d,
                startAtSeconds: 0d,
                windupSeconds: 0.1d,
                activeSeconds: 0.2d,
                recoverySeconds: 0.1d,
                targetActorId: new ActorId(2UL));
            Assert.That(action.IsActivated, Is.False);
            Assert.That(action.IsCompleted, Is.False);
            action.MarkActivated();
            Assert.That(action.IsActivated, Is.True);
            Assert.That(action.IsCompleted, Is.False);
            action.MarkCompleted();
            Assert.That(action.IsActivated, Is.True);
            Assert.That(action.IsCompleted, Is.True);
        }

        // ----- EquipmentState.Unequip -----
        [Test]
        public void EquipmentState_Unequip_RemovesEquippedItem()
        {
            var eq = new EquipmentState();
            eq.Equip(EquipmentSlot.Weapon, new ItemId(42UL));
            Assert.That(eq.GetEquippedItemId(EquipmentSlot.Weapon).Value, Is.EqualTo(42UL));
            eq.Unequip(EquipmentSlot.Weapon);
            Assert.That(eq.GetEquippedItemId(EquipmentSlot.Weapon).Value, Is.EqualTo(0UL));
        }

        [Test]
        public void EquipmentState_Unequip_NoopOnUnEquippedSlot()
        {
            var eq = new EquipmentState();
            Assert.DoesNotThrow(() => eq.Unequip(EquipmentSlot.Weapon));
            Assert.That(eq.GetEquippedItemId(EquipmentSlot.Weapon).Value, Is.EqualTo(0UL));
        }

        // ----- InventoryItem.AddQuantity / RemoveQuantity -----
        [Test]
        public void InventoryItem_AddQuantity_RejectsNegative()
        {
            var item = new InventoryItem(new ItemId(1UL), "iron", "Iron", 5);
            item.AddQuantity(-3); // negative is clamped to 0 internally
            Assert.That(item.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void InventoryItem_AddQuantity_AccumulatesPositive()
        {
            var item = new InventoryItem(new ItemId(1UL), "iron", "Iron", 5);
            item.AddQuantity(7);
            Assert.That(item.Quantity, Is.EqualTo(12));
        }

        [Test]
        public void InventoryItem_RemoveQuantity_FloorsAtZero()
        {
            var item = new InventoryItem(new ItemId(1UL), "iron", "Iron", 5);
            item.RemoveQuantity(99);
            Assert.That(item.Quantity, Is.EqualTo(0));
        }

        // ----- ActorMemory mutators -----
        [Test]
        public void ActorMemory_MarkDialogueSeen_DeduplicatesTopic()
        {
            var mem = new ActorMemory(new ActorId(1UL));
            mem.MarkDialogueSeen("greet");
            mem.MarkDialogueSeen("greet");
            mem.MarkDialogueSeen("trade");
            Assert.That(mem.DialogueSeen.Count, Is.EqualTo(2));
            Assert.That(mem.HasDialogueSeen("greet"), Is.True);
            Assert.That(mem.HasDialogueSeen("trade"), Is.True);
        }

        [Test]
        public void ActorMemory_ReplaceEvents_OverridesList()
        {
            var mem = new ActorMemory(new ActorId(1UL));
            mem.ReplaceEvents(new[]
            {
                new InteractionEvent(new GameTime(0), "test", new ActorId(2UL), "subj-a", string.Empty, 0, new GridPosition(0, 0)),
                new InteractionEvent(new GameTime(1), "test", new ActorId(3UL), "subj-b", string.Empty, 0, new GridPosition(0, 0)),
            });
            Assert.That(mem.Events.Count, Is.EqualTo(2));
            mem.ReplaceEvents(System.Array.Empty<InteractionEvent>());
            Assert.That(mem.Events.Count, Is.EqualTo(0));
        }

        [Test]
        public void ActorMemory_ReplaceDialogueSeen_Roundtrips()
        {
            var mem = new ActorMemory(new ActorId(1UL));
            mem.MarkDialogueSeen("old");
            mem.ReplaceDialogueSeen(new[] { "alpha", "beta" });
            Assert.That(mem.HasDialogueSeen("old"), Is.False);
            Assert.That(mem.HasDialogueSeen("alpha"), Is.True);
            Assert.That(mem.HasDialogueSeen("beta"), Is.True);
        }

        [Test]
        public void ActorMemory_ReplaceTransactions_PreservesOrder()
        {
            var mem = new ActorMemory(new ActorId(1UL));
            mem.ReplaceTransactions(new[]
            {
                new TransactionRecord(new GameTime(0), "trade", "ore", 1, -5),
                new TransactionRecord(new GameTime(1), "trade", "bread", 2, -3),
            });
            Assert.That(mem.Transactions.Count, Is.EqualTo(2));
            Assert.That(mem.Transactions[0].ItemTemplateId, Is.EqualTo("ore"));
            Assert.That(mem.Transactions[1].ItemTemplateId, Is.EqualTo("bread"));
        }

        // ----- NpcMemoryStore.ReplaceAll -----
        [Test]
        public void NpcMemoryStore_ReplaceAll_OverridesPriorMemories()
        {
            var store = new NpcMemoryStore();
            var m1 = new ActorMemory(new ActorId(1UL));
            m1.MarkDialogueSeen("stale");
            store.GetOrCreate(new ActorId(1UL)).MarkDialogueSeen("stale");

            var fresh = new ActorMemory(new ActorId(5UL));
            fresh.MarkDialogueSeen("hello");
            store.ReplaceAll(new[] { fresh });

            Assert.That(store.TryGet(new ActorId(1UL), out _), Is.False);
            Assert.That(store.TryGet(new ActorId(5UL), out var got), Is.True);
            Assert.That(got.HasDialogueSeen("hello"), Is.True);
        }

        // ----- JobBoard.TryRestoreClaim -----
        [Test]
        public void JobBoard_TryRestoreClaim_RejectsEmptyIds()
        {
            var board = new JobBoard();
            Assert.That(board.TryRestoreClaim(default, new ActorId(1UL), 5), Is.False);
            Assert.That(board.TryRestoreClaim(new JobId(1UL), default, 5), Is.False);
        }

        [Test]
        public void JobBoard_TryRestoreClaim_RejectsUnknownJob()
        {
            var board = new JobBoard();
            Assert.That(board.TryRestoreClaim(new JobId(99UL), new ActorId(1UL), 1), Is.False);
        }

        // ----- CaravanInstance.Load -----
        [Test]
        public void CaravanInstance_Load_RejectsNegativeQuantity()
        {
            var caravan = new CaravanInstance(
                new CaravanId(1UL), new TradeRouteId(1UL),
                new SiteId(1UL), 0, 0, CaravanState.Idle);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => caravan.Load(-1));
        }

        [Test]
        public void CaravanInstance_Load_AccumulatesAndTransitionsToEnRoute()
        {
            var caravan = new CaravanInstance(
                new CaravanId(1UL), new TradeRouteId(1UL),
                new SiteId(1UL), 0, 0, CaravanState.Idle);
            caravan.Load(3);
            Assert.That(caravan.PayloadRemaining, Is.EqualTo(3));
            Assert.That(caravan.State, Is.EqualTo(CaravanState.EnRoute));
            caravan.Load(2);
            Assert.That(caravan.PayloadRemaining, Is.EqualTo(5));
        }

        [Test]
        public void CaravanInstance_Load_ZeroQuantityDoesNotTransition()
        {
            var caravan = new CaravanInstance(
                new CaravanId(1UL), new TradeRouteId(1UL),
                new SiteId(1UL), 0, 0, CaravanState.Idle);
            caravan.Load(0);
            Assert.That(caravan.PayloadRemaining, Is.EqualTo(0));
            Assert.That(caravan.State, Is.EqualTo(CaravanState.Idle));
        }
    }
}
