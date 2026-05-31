using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class PromptComposerTests
    {
        [Test]
        public void NpcPortrait_SameInput_SamePromptAndHash()
        {
            var profile = Profile(WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue);
            var npc = new NpcSeedRecord(new NpcId(7), new SettlementId(2), new FactionId(3), "Brennec the Forge-Walker", 931, NpcRole.Artisan);

            var a = PromptComposers.NpcPortrait(npc, profile);
            var b = PromptComposers.NpcPortrait(npc, profile);

            Assert.That(a.Prompt, Is.EqualTo(b.Prompt));
            Assert.That(a.PromptHash, Is.EqualTo(b.PromptHash));
            Assert.That(a.Subject, Is.EqualTo(AssetSubjectKind.Npc));
        }

        [Test]
        public void NpcPortrait_StyleGenre_AltersPromptDeterministically()
        {
            var npc = new NpcSeedRecord(new NpcId(7), new SettlementId(2), new FactionId(3), "Brennec", 931, NpcRole.Artisan);

            var dark = PromptComposers.NpcPortrait(npc, Profile(WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue));
            var high = PromptComposers.NpcPortrait(npc, Profile(WorldStyle.HighFantasy, WorldGenre.Pilgrimage));

            Assert.That(dark.Prompt, Does.Contain("DarkFantasyGrim"));
            Assert.That(high.Prompt, Does.Contain("HighFantasy"));
            Assert.That(dark.PromptHash, Is.Not.EqualTo(high.PromptHash));
        }

        [Test]
        public void ItemAndRegion_ComposersProduceStableCacheKeys()
        {
            var profile = Profile(WorldStyle.LowFantasy, WorldGenre.MerchantEmpire);
            var item = new ItemRecord(new ItemId(44), ItemMaterial.Iron, ItemQuality.Common, EquipmentSlot.Weapon);
            var region = new RegionRecord(new RegionId(55), "Ash Vale", 1, 2, BiomeKind.AridSteppe);

            Assert.That(PromptComposers.CacheKey(PromptComposers.ItemIcon(item, profile)), Has.Length.EqualTo(64));
            Assert.That(PromptComposers.CacheKey(PromptComposers.RegionEstablishingShot(region, profile)), Has.Length.EqualTo(64));
        }

        private static WorldProfile Profile(WorldStyle style, WorldGenre genre)
        {
            return new WorldProfile(style, genre, 42, 1000000, 47, 12, 100, "grim", "mage", "city");
        }
    }
}
