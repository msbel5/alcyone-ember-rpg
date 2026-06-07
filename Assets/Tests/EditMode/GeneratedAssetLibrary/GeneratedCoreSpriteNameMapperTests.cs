using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.GeneratedAssetLibrary
{
    public sealed class GeneratedCoreSpriteNameMapperTests
    {
        [Test]
        public void TryMap_MapsInventoryTemplatesToGeneratedItems()
        {
            Assert.That(GeneratedCoreSpriteNameMapper.TryMap("steel_longsword", out var sword), Is.True);
            Assert.That(sword, Is.EqualTo("item_sword"));

            Assert.That(GeneratedCoreSpriteNameMapper.TryMap("healing_potion", out var potion), Is.True);
            Assert.That(potion, Is.EqualTo("item_potion"));
        }

        [Test]
        public void TryMap_MapsSpellsAndUiIconsToGeneratedCoreIds()
        {
            Assert.That(GeneratedCoreSpriteNameMapper.TryMap("fire", out var fire), Is.True);
            Assert.That(fire, Is.EqualTo("spell_fire"));

            Assert.That(GeneratedCoreSpriteNameMapper.TryMap("inventory", out var inventory), Is.True);
            Assert.That(inventory, Is.EqualTo("inventory"));
        }

        [Test]
        public void TryMap_UnknownItemFallsBackToGeneratedInventoryIcon()
        {
            Assert.That(GeneratedCoreSpriteNameMapper.TryMap("iron_ore", out var coreId), Is.True);
            Assert.That(coreId, Is.EqualTo("inventory"));
        }
    }
}
