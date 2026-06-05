using EmberCrpg.Data.Save;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class WorldSaveMapperTradeFieldsTests
    {
        [Test]
        public void RoundTrip_PreservesTradeFields()
        {
            var world = new WorldFactory().Create(99);
            world.PlayerGold = 345;
            world.MerchantGold = 789;
            world.MerchantStoreSeeded = true;

            var data = WorldSaveMapper.ToData(world);
            var restored = WorldSaveMapper.ToWorld(data, new WorldFactory().Create(99));

            Assert.That(restored.PlayerGold, Is.EqualTo(345));
            Assert.That(restored.MerchantGold, Is.EqualTo(789));
            Assert.That(restored.MerchantStoreSeeded, Is.True);
        }
    }
}
