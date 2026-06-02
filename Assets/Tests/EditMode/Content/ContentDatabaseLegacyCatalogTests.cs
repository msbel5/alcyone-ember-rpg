using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Content
{
    public sealed class ContentDatabaseLegacyCatalogTests
    {
        [Test]
        public void LoadFromStreamingAssets_ExposesExpectedLegacyCatalogSizes()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.Items.Count, Is.GreaterThan(800));
            Assert.That(db.Recipes.Count, Is.GreaterThan(80));
            Assert.That(db.Materials.Count, Is.GreaterThan(10));
            Assert.That(db.Factions.Count, Is.GreaterThan(8));
            Assert.That(db.Classes.Count, Is.GreaterThan(8));
            Assert.That(db.Spells.Count, Is.GreaterThan(300));
            Assert.That(db.Monsters.Count, Is.GreaterThan(300));
            Assert.That(db.Locations.Count, Is.GreaterThan(200));
            Assert.That(db.NpcTemplates.Count, Is.GreaterThan(200));
        }

        [Test]
        public void LoadFromStreamingAssets_DeserializesKnownLegacySamples()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.Items["aberrant_flesh"].rarity, Is.EqualTo("RARE"));
            Assert.That(db.Recipes["iron_bar"].ingredients[0].item_id, Is.EqualTo("iron_ore"));
            Assert.That(db.Materials["iron"].impact_yield, Is.EqualTo(180));
            Assert.That(db.Factions["harbor_guard"].ethics["THEFT"], Is.EqualTo("crime"));
            Assert.That(db.Classes["warrior"].hit_die_size, Is.EqualTo(10));
            Assert.That(db.Spells["magic_missile"].effects[0].type, Is.EqualTo("damage"));
            Assert.That(db.Monsters["wolf"].attacks[0].damage_dice, Is.EqualTo("2d4+2"));
            Assert.That(db.Locations["harbor_town"].danger_level, Is.EqualTo(0));
            Assert.That(db.NpcTemplates["ally_ranger"].dialogue["greeting"].Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoadFromSameRoot_RemainsDeterministic()
        {
            var first = ContentDatabaseTestContext.Load();
            var second = ContentDatabaseTestContext.Load();

            Assert.That(second.Items.Count, Is.EqualTo(first.Items.Count));
            Assert.That(second.Recipes["iron_bar"].products[0].item_id, Is.EqualTo(first.Recipes["iron_bar"].products[0].item_id));
            Assert.That(second.LocationCatalog.opening_scenes[0].location, Is.EqualTo(first.LocationCatalog.opening_scenes[0].location));
        }
    }
}
