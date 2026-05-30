using EmberCrpg.Domain.Inventory;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin the deterministic text emitted for the Sprint 4 inventory/equipment UI.
// They cover inspect and HUD equipment lines, not Unity OnGUI rendering.
namespace EmberCrpg.Tests.EditMode.Presentation
{
    /// <summary>Verifies player-facing inventory/equipment formatter output.</summary>
    public sealed class InventoryEquipmentFormatterTests
    {
        [Test]
        public void FormatInspect_ShowsInventoryItemAndEmptyWeaponSlot()
        {
            var world = new WorldFactory().Create(1337);

            var text = InventoryEquipmentFormatter.FormatInspect(world);

            Assert.That(text, Does.Contain("Ash Training Blade"));
            Assert.That(text, Does.Contain("Weapon: none"));
        }

        [Test]
        public void FormatEquipmentLine_ShowsEquippedWeaponBonuses()
        {
            var world = new WorldFactory().Create(1337);
            var weapon = world.PlayerInventory.FindFirstEquipment(EquipmentSlot.Weapon);
            new EquipmentService().TryEquip(world.PlayerInventory, world.PlayerEquipment, weapon.Id);

            var text = InventoryEquipmentFormatter.FormatEquipmentLine(world);

            Assert.That(text, Does.Contain("Ash Training Blade"));
            Assert.That(text, Does.Contain("+5 ACC"));
            Assert.That(text, Does.Contain("+2 DMG"));
        }
    }
}
