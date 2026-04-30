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
        public void SaveAndLoad_RoundTripsDoorMerchantGuardAndEnemyState()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.Player.MoveTo(world.Merchant.Position.Translate(0, 1));
            world.PlayerInventory.TryAdd(world.Pickups[0].Item);
            world.Pickups[0].Collect();
            new MerchantTradeService().TradeGateWrit(world);
            new AskAboutService().Ask(world, "embers");
            new AskAboutService().Ask(world, "embers");
            world.Player.MoveTo(world.Guard.Position.Translate(0, -1));
            new GuardInteractionService().Interact(world);
            world.Player.MoveTo(new GridPosition(world.Room.DoorCell.X, 1));
            new DoorInteractionService().Toggle(world);
            world.Enemy.ApplyVitals(world.Enemy.Vitals.WithHealth(world.Enemy.Vitals.Health.Damage(5)));

            var service = new EmberCrpg.Data.Save.JsonSliceSaveService();
            var json = service.SaveToJson(world);
            var loaded = service.LoadFromJson(json);

            Assert.That(loaded.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.True);
            Assert.That(loaded.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.False);
            Assert.That(loaded.MerchantInventory.Contains(SliceItemCatalog.EmberShardTemplateId), Is.True);
            Assert.That(loaded.GuardDoorAccessGranted, Is.True);
            Assert.That(loaded.DoorOpen, Is.True);
            Assert.That(loaded.Enemy.Vitals.Health.Current, Is.EqualTo(world.Enemy.Vitals.Health.Current));
            Assert.That(loaded.NpcMemory.TryGet(loaded.Talker.Id, out var talkerMemory), Is.True);
            Assert.That(talkerMemory.HasDialogueSeen("embers"), Is.True);
            Assert.That(talkerMemory.CountEvents(ActorMemoryEventTypes.DialogueTopic), Is.EqualTo(2));
            Assert.That(loaded.NpcMemory.TryGet(loaded.Merchant.Id, out var merchantMemory), Is.True);
            Assert.That(merchantMemory.Transactions.Single().ItemTemplateId, Is.EqualTo(SliceItemCatalog.GateWritTemplateId));
            Assert.That(loaded.NpcMemory.TryGet(loaded.Guard.Id, out var guardMemory), Is.True);
            Assert.That(guardMemory.CountEvents(ActorMemoryEventTypes.ClearanceGranted), Is.EqualTo(1));
        }
    }
}
