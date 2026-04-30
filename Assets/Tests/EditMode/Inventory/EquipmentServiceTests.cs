using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin Sprint 3's weapon/armor separation and deterministic equipment swaps.
// They cover exact-item equipping, replacement, and inventory rollback semantics.
namespace EmberCrpg.Tests.EditMode.Inventory
{
    /// <summary>Verifies equip/unequip behavior for the slice player.</summary>
    public sealed class EquipmentServiceTests
    {
        [Test]
        public void Equip_MovesExactWeaponFromInventoryIntoSlot()
        {
            var world = new SliceWorldFactory().Create(1337);
            var blade = SliceItemCatalog.CreateWardenBlade(world.ItemIds);
            world.PlayerInventory.TryAdd(blade);

            var reply = new EquipmentService().Equip(world, blade.Id);

            Assert.That(reply, Does.Contain("equip Warden Blade"));
            Assert.That(world.PlayerEquipment.Weapon.Id, Is.EqualTo(blade.Id));
            Assert.That(world.PlayerInventory.Contains(blade.Id), Is.False);
        }

        [Test]
        public void Equip_ReplacingWeapon_StowsPreviousInstanceBackIntoInventory()
        {
            var world = new SliceWorldFactory().Create(1337);
            var starterWeaponId = world.PlayerEquipment.Weapon.Id;
            var replacement = new InventoryItem(new ItemId(999), "spare_blade", "Spare Blade", 1, false, EquipmentSlot.Weapon);
            world.PlayerInventory.TryAdd(replacement);

            var reply = new EquipmentService().Equip(world, replacement.Id);

            Assert.That(reply, Does.Contain("stow Warden Blade"));
            Assert.That(world.PlayerEquipment.Weapon.Id, Is.EqualTo(replacement.Id));
            Assert.That(world.PlayerInventory.Contains(starterWeaponId), Is.True);
        }

        [Test]
        public void Unequip_WhenInventoryFull_LeavesArmorInSlot()
        {
            var world = new SliceWorldFactory().Create(1337);
            world.PlayerInventory = new InventoryState(1);
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(55), "satchel_note", "Satchel Note", 1, false));

            var reply = new EquipmentService().Unequip(world, EquipmentSlot.Armor);

            Assert.That(reply, Does.Contain("too full"));
            Assert.That(world.PlayerEquipment.Armor, Is.Not.Null);
        }
    }
}
