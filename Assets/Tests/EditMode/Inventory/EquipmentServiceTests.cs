using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.Rng;
using EmberCrpg.Simulation.World;
using NUnit.Framework;
using EmberCrpg.Domain.Actors;

// Design note:
// These tests pin Sprint 4 Faz 4 equipment rules and the first mechanic affected by gear.
// They cover equip success/refusal, slot constraints, and combat damage modifiers.
namespace EmberCrpg.Tests.EditMode.Inventory
{
    /// <summary>Verifies deterministic equipment behavior without UnityEngine.</summary>
    public sealed class EquipmentServiceTests
    {
        [Test]
        public void TryEquip_WeaponInInventory_EquipsByStableItemId()
        {
            var world = new SliceWorldFactory().Create(1337);
            var weapon = world.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon);

            var result = new EquipmentService().TryEquip(world.PlayerInventory, world.PlayerEquipment, weapon.Id);

            Assert.That(result.Success, Is.True);
            Assert.That(world.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon), Is.EqualTo(weapon.Id));
        }

        [Test]
        public void TryEquip_NonEquipmentItem_ReturnsDeterministicRefusal()
        {
            var inventory = new InventoryState(10);
            var equipment = new EquipmentState();
            var shard = SliceItemCatalog.CreateEmberShard();
            inventory.TryAdd(shard);

            var result = new EquipmentService().TryEquip(inventory, equipment, shard.Id);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(EquipmentActionError.ItemNotEquippable));
            Assert.That(equipment.GetEquippedItemId(EquipmentSlot.Weapon).IsEmpty, Is.True);
        }

        [Test]
        public void TryEquip_WhenSlotOccupied_RefusesSecondWeaponUntilUnequipped()
        {
            var inventory = new InventoryState(10);
            var equipment = new EquipmentState();
            var first = SliceItemCatalog.CreateAshTrainingBlade();
            var second = new InventoryItem(new ItemId(3002), "ash_training_blade_spare", "Spare Ash Blade", 1, EquipmentSlot.Weapon, 1, 1);
            inventory.TryAdd(first);
            inventory.TryAdd(second);
            var service = new EquipmentService();

            service.TryEquip(inventory, equipment, first.Id);
            var refused = service.TryEquip(inventory, equipment, second.Id);

            Assert.That(refused.Success, Is.False);
            Assert.That(refused.Error, Is.EqualTo(EquipmentActionError.SlotOccupied));
            Assert.That(equipment.GetEquippedItemId(EquipmentSlot.Weapon), Is.EqualTo(first.Id));
        }


        [Test]
        public void TryEquip_AlreadyEquippedWeapon_ReturnsDedicatedErrorCode()
        {
            var world = new SliceWorldFactory().Create(1337);
            var weapon = world.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon);
            var service = new EquipmentService();

            service.TryEquip(world.PlayerInventory, world.PlayerEquipment, weapon.Id);
            var result = service.TryEquip(world.PlayerInventory, world.PlayerEquipment, weapon.Id);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(EquipmentActionError.AlreadyEquipped));
            Assert.That(world.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon), Is.EqualTo(weapon.Id));
        }

        [Test]
        public void TryRemove_WithEquipmentState_RefusesToRemoveEquippedItem()
        {
            var world = new SliceWorldFactory().Create(1337);
            var weapon = world.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon);
            var service = new EquipmentService();
            service.TryEquip(world.PlayerInventory, world.PlayerEquipment, weapon.Id);

            var removed = world.PlayerInventory.TryRemove(weapon.TemplateId, weapon.Quantity, world.PlayerEquipment);

            Assert.That(removed, Is.False);
            Assert.That(world.PlayerInventory.FindById(weapon.Id), Is.Not.Null);
            Assert.That(world.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon), Is.EqualTo(weapon.Id));
        }

        [Test]
        public void TryUnequip_EquippedWeapon_ClearsSlot()
        {
            var world = new SliceWorldFactory().Create(1337);
            var weapon = world.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon);
            var service = new EquipmentService();
            service.TryEquip(world.PlayerInventory, world.PlayerEquipment, weapon.Id);

            var result = service.TryUnequip(world.PlayerEquipment, EquipmentSlot.Weapon);

            Assert.That(result.Success, Is.True);
            Assert.That(world.PlayerEquipment.GetEquippedItemId(EquipmentSlot.Weapon).IsEmpty, Is.True);
        }

        [Test]
        public void EquippedWeapon_IncreasesEncounterStrikeDamage()
        {
            var plain = new SliceWorldFactory().Create(1337);
            var geared = new SliceWorldFactory().Create(1337);
            var service = new EquipmentService();
            var weapon = geared.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon);
            service.TryEquip(geared.PlayerInventory, geared.PlayerEquipment, weapon.Id);
            var turn = new EncounterTurnService();

            var plainStrike = turn.Advance(new EmberCrpg.Domain.Combat.EncounterState(plain.Actors.FirstByRole(ActorRole.Player).Id, plain.Actors.FirstByRole(ActorRole.Enemy).Id), plain.Actors.FirstByRole(ActorRole.Player), plain.Actors.FirstByRole(ActorRole.Enemy), new FixedRng(1));
            var gearStats = service.GetCombatStats(geared.PlayerInventory, geared.PlayerEquipment);
            var gearedStrike = turn.Advance(new EmberCrpg.Domain.Combat.EncounterState(geared.Actors.FirstByRole(ActorRole.Player).Id, geared.Actors.FirstByRole(ActorRole.Enemy).Id), geared.Actors.FirstByRole(ActorRole.Player), geared.Actors.FirstByRole(ActorRole.Enemy), new FixedRng(1), gearStats, new EquipmentCombatStats(0, 0));

            Assert.That(gearedStrike.Hit, Is.True);
            Assert.That(gearedStrike.RawDamage, Is.GreaterThan(plainStrike.RawDamage));
            Assert.That(gearedStrike.MitigatedDamage, Is.GreaterThan(plainStrike.MitigatedDamage));
        }

        private sealed class FixedRng : IDeterministicRng
        {
            private readonly int _roll;

            public FixedRng(int roll) { _roll = roll; }
            public int NextInt(int exclusiveMax) { return 0; }
            public int RollPercent() { return _roll; }
        }
    }
}
