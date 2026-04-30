using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Narrative;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin JSON save/load round-trips for the slice world snapshot.
// They cover deterministic data preservation, not filesystem IO.
namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>Verifies JSON round-tripping of the vertical slice state.</summary>
    public sealed class JsonSliceSaveServiceTests
    {
        [Test]
        public void SaveAndLoad_RoundTripsDoorMerchantGuardEnemyMemoryAndNextItemId()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Talker.Position.Translate(0, -1));
            new AskAboutService().Ask(world, "gate");
            world.Player.MoveTo(world.Merchant.Position.Translate(0, 1));
            world.PlayerInventory.TryAdd(world.Pickups[0].Item);
            world.Pickups[0].Collect();
            new MerchantTradeService().TradeGateWrit(world);
            world.Player.MoveTo(world.Guard.Position.Translate(0, -1));
            new GuardInteractionService().Interact(world);
            world.Player.MoveTo(new GridPosition(world.Room.DoorCell.X, 1));
            new DoorInteractionService().Toggle(world);
            world.Enemy.ApplyVitals(world.Enemy.Vitals.WithHealth(world.Enemy.Vitals.Health.Damage(5)));
            var expectedNextId = world.ItemIds.Clone().TakeNext();

            var service = new EmberCrpg.Data.Save.JsonSliceSaveService();
            var json = service.SaveToJson(world);
            var loaded = service.LoadFromJson(json);
            var loadedNextId = SliceItemCatalog.CreateGateWrit(loaded.ItemIds).Id;
            ActorMemory talkerMemory;
            ActorMemory merchantMemory;
            ActorMemory guardMemory;

            Assert.That(loaded.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.True);
            Assert.That(loaded.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.False);
            Assert.That(loaded.MerchantInventory.Contains(SliceItemCatalog.EmberShardTemplateId), Is.True);
            Assert.That(loaded.GuardDoorAccessGranted, Is.True);
            Assert.That(loaded.DoorOpen, Is.True);
            Assert.That(loaded.Enemy.Vitals.Health.Current, Is.EqualTo(world.Enemy.Vitals.Health.Current));
            Assert.That(loadedNextId, Is.EqualTo(expectedNextId));
            Assert.That(loaded.NpcMemories.TryGet(loaded.Talker.Id, out talkerMemory), Is.True);
            Assert.That(loaded.NpcMemories.TryGet(loaded.Merchant.Id, out merchantMemory), Is.True);
            Assert.That(loaded.NpcMemories.TryGet(loaded.Guard.Id, out guardMemory), Is.True);
            Assert.That(talkerMemory.DialogueSeen, Does.Contain("gate"));
            Assert.That(merchantMemory.Events.Last().Type, Is.EqualTo(ActorMemoryEventType.TradeCompleted));
            Assert.That(guardMemory.Events.Last().Type, Is.EqualTo(ActorMemoryEventType.DoorClearanceGranted));
        }
    }
}
