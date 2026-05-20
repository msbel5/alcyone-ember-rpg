using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
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
            world.Actors.FirstByRole(ActorRole.Player).MoveTo(world.Actors.FirstByRole(ActorRole.Merchant).Position.Translate(0, 1));
            var weapon = world.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon);
            new EquipmentService().TryEquip(world.PlayerInventory, world.PlayerEquipment, weapon.Id);
            world.PlayerInventory.TryAdd(world.Pickups[0].Item);
            world.Pickups[0].Collect();
            new MerchantTradeService().TradeGateWrit(world);
            new AskAboutService().Ask(world, "embers");
            new AskAboutService().Ask(world, "embers");
            world.Actors.FirstByRole(ActorRole.Player).MoveTo(world.Actors.FirstByRole(ActorRole.Guard).Position.Translate(0, -1));
            new GuardInteractionService().Interact(world);
            world.Actors.FirstByRole(ActorRole.Player).MoveTo(new GridPosition(world.Room.DoorCell.X, 1));
            new DoorInteractionService().Toggle(world);
            world.Actors.FirstByRole(ActorRole.Enemy).ApplyVitals(world.Actors.FirstByRole(ActorRole.Enemy).Vitals.WithHealth(world.Actors.FirstByRole(ActorRole.Enemy).Vitals.Health.Damage(5)));
            world.PlayerSpellCooldowns.SetRemainingTicks("ember.spark", 4);
            world.PlayerSpellCooldowns.SetRemainingTicks("ash.bind", 2);
            world.PlayerShieldBuffs.SetActiveBuff("ember_ward", 30, 4);
            world.PlayerShieldBuffs.SetActiveBuff("ash.bind", 6, 1);
            var farRoomState = world.DungeonRoomStates.Last();
            farRoomState.Visited = true;
            farRoomState.Cleared = true;
            world.CurrentRoomId = farRoomState.RoomId;
            var farDoorState = world.DungeonDoorStates.Last();
            farDoorState.Open = false;

            var service = new EmberCrpg.Data.Save.JsonSliceSaveService();
            var json = service.SaveToJson(world);
            var loaded = service.LoadFromJson(json);

            Assert.That(loaded.PlayerInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.True);
            Assert.That(loaded.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon).Id, Is.EqualTo(weapon.Id));
            Assert.That(loaded.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon), Is.EqualTo(weapon.Id));
            Assert.That(loaded.MerchantInventory.Contains(SliceItemCatalog.GateWritTemplateId), Is.False);
            Assert.That(loaded.MerchantInventory.Contains(SliceItemCatalog.EmberShardTemplateId), Is.True);
            Assert.That(loaded.GuardDoorAccessGranted, Is.True);
            Assert.That(loaded.DoorOpen, Is.True);
            Assert.That(loaded.Actors.FirstByRole(ActorRole.Enemy).Vitals.Health.Current, Is.EqualTo(world.Actors.FirstByRole(ActorRole.Enemy).Vitals.Health.Current));
            Assert.That(loaded.Dungeon.Rooms.Count, Is.EqualTo(world.Dungeon.Rooms.Count));
            Assert.That(loaded.Dungeon.Doors.Select(door => door.Id), Is.EqualTo(world.Dungeon.Doors.Select(door => door.Id)));
            Assert.That(loaded.CurrentRoomId, Is.EqualTo(world.CurrentRoomId));
            Assert.That(loaded.EnemyRoomId, Is.EqualTo(world.EnemyRoomId));
            Assert.That(loaded.MerchantRoomId, Is.EqualTo(world.MerchantRoomId));
            Assert.That(loaded.TalkerRoomId, Is.EqualTo(world.TalkerRoomId));
            Assert.That(loaded.Dungeon.FindSpawn(DungeonSpawnKind.Enemy).RoomId, Is.EqualTo(world.EnemyRoomId));
            Assert.That(loaded.DungeonRoomStates.Last().Cleared, Is.True);
            Assert.That(loaded.DungeonDoorStates.Last().Open, Is.False);
            Assert.That(loaded.NpcMemory.TryGet(loaded.Actors.FirstByRole(ActorRole.Talker).Id, out var talkerMemory), Is.True);
            Assert.That(talkerMemory.HasDialogueSeen("embers"), Is.True);
            Assert.That(talkerMemory.CountEvents(ActorMemoryEventTypes.DialogueTopic), Is.EqualTo(2));
            Assert.That(loaded.NpcMemory.TryGet(loaded.Actors.FirstByRole(ActorRole.Merchant).Id, out var merchantMemory), Is.True);
            Assert.That(merchantMemory.Transactions.Single().ItemTemplateId, Is.EqualTo(SliceItemCatalog.GateWritTemplateId));
            Assert.That(loaded.NpcMemory.TryGet(loaded.Actors.FirstByRole(ActorRole.Guard).Id, out var guardMemory), Is.True);
            Assert.That(guardMemory.CountEvents(ActorMemoryEventTypes.ClearanceGranted), Is.EqualTo(1));
            Assert.That(loaded.PlayerSpellCooldowns, Is.Not.Null);
            Assert.That(loaded.PlayerSpellCooldowns.GetRemainingTicks("ember.spark"), Is.EqualTo(4));
            Assert.That(loaded.PlayerSpellCooldowns.GetRemainingTicks("ash.bind"), Is.EqualTo(2));
            Assert.That(loaded.PlayerSpellCooldowns.GetTrackedSpellTemplateIds().Count, Is.EqualTo(2));
            Assert.That(loaded.PlayerShieldBuffs, Is.Not.Null);
            Assert.That(loaded.PlayerShieldBuffs.GetRemainingTicks("ember_ward"), Is.EqualTo(30));
            Assert.That(loaded.PlayerShieldBuffs.GetMagnitude("ember_ward"), Is.EqualTo(4));
            Assert.That(loaded.PlayerShieldBuffs.GetRemainingTicks("ash.bind"), Is.EqualTo(6));
            Assert.That(loaded.PlayerShieldBuffs.GetMagnitude("ash.bind"), Is.EqualTo(1));
            Assert.That(loaded.PlayerShieldBuffs.GetTrackedSpellTemplateIds().Count, Is.EqualTo(2));
        }

        [Test]
        public void SaveAndLoad_FreshWorld_StartsWithNoSpellCooldowns()
        {
            var world = new SliceWorldFactory().Create(2027);

            var service = new EmberCrpg.Data.Save.JsonSliceSaveService();
            var loaded = service.LoadFromJson(service.SaveToJson(world));

            Assert.That(loaded.PlayerSpellCooldowns, Is.Not.Null);
            Assert.That(loaded.PlayerSpellCooldowns.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }

        [Test]
        public void SaveAndLoad_FreshWorld_StartsWithNoShieldBuffs()
        {
            var world = new SliceWorldFactory().Create(2027);

            var service = new EmberCrpg.Data.Save.JsonSliceSaveService();
            var loaded = service.LoadFromJson(service.SaveToJson(world));

            Assert.That(loaded.PlayerShieldBuffs, Is.Not.Null);
            Assert.That(loaded.PlayerShieldBuffs.GetTrackedSpellTemplateIds().Count, Is.EqualTo(0));
        }
    }
}
