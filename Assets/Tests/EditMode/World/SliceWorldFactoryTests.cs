using System.Linq;
using EmberCrpg.Simulation.Inventory;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// These tests pin role-differentiated loadouts plus Sprint 3 world hardening.
// They cover deterministic starting distinctions, item ids, equipment, and npc memory seeding.
namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies initial world state for the vertical slice.</summary>
    public sealed class SliceWorldFactoryTests
    {
        [Test]
        public void Create_AssignsDistinctRoleVitalsAndCombatFields()
        {
            var world = new SliceWorldFactory().Create(1337);
            Assert.That(world.Player.Vitals.Health.Max, Is.GreaterThan(world.Talker.Vitals.Health.Max));
            Assert.That(world.Guard.Armor, Is.GreaterThan(world.Merchant.Armor));
            Assert.That(world.Merchant.Stats.Pre, Is.GreaterThan(world.Enemy.Stats.Pre));
            Assert.That(world.Enemy.Dodge, Is.GreaterThan(world.Guard.Dodge));
        }

        [Test]
        public void Create_SeedsUniqueItemIdsEquipmentAndNpcMemoryStore()
        {
            var world = new SliceWorldFactory().Create(1337);
            var itemIds = new[]
            {
                world.PlayerEquipment.Weapon.Id.Value,
                world.PlayerEquipment.Armor.Id.Value,
                world.MerchantInventory.Items[0].Id.Value,
                world.Pickups[0].Item.Id.Value,
            };

            Assert.That(itemIds.Distinct().Count(), Is.EqualTo(itemIds.Length));
            Assert.That(world.ItemIds.NextValue, Is.EqualTo(5u));
            Assert.That(world.NpcMemories.Entries.Count, Is.EqualTo(3));
        }
    }
}
