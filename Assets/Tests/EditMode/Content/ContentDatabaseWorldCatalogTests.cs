using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Content
{
    public sealed class ContentDatabaseWorldCatalogTests
    {
        [Test]
        public void LoadFromStreamingAssets_ExposesWorldCatalogs()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.Worldgen.town_building_types.Count, Is.GreaterThan(0));
            Assert.That(db.Biomes.Count, Is.GreaterThan(0));
            Assert.That(db.Cultures.Count, Is.GreaterThan(0));
            Assert.That(db.WorldBuildingTemplates.Count, Is.GreaterThan(0));
            Assert.That(db.SpeciesTemplates.Count, Is.GreaterThan(0));
            Assert.That(db.WorldQuestTemplates.Count, Is.GreaterThan(0));
            Assert.That(db.WorldFurniture.Count, Is.GreaterThan(0));
            Assert.That(db.WorldProfiles.Count, Is.GreaterThan(0));
            Assert.That(db.WorldNpcTemplates.Count, Is.GreaterThan(0));
            Assert.That(db.WorldAdapters.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoadFromStreamingAssets_DeserializesWorldCatalogSamples()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.Worldgen.town_building_types[0], Is.EqualTo("inn"));
            Assert.That(db.Biomes["coast"].resources[0], Is.EqualTo("salt"));
            Assert.That(db.Cultures["civic_humans"].governance_bias, Is.EqualTo("council"));
            Assert.That(db.WorldBuildingTemplates["blacksmith"].npc_roles[0], Is.EqualTo("smith"));
            Assert.That(db.SpeciesTemplates["human"].name, Is.EqualTo("Human"));
            Assert.That(db.WorldQuestTemplates["fetch"].title, Is.EqualTo("Fetch {resource} For The Stores"));
            Assert.That(db.WorldFurniture["forge"].interaction_type, Is.EqualTo("craft"));
            Assert.That(db.WorldProfiles["standard"].world_width, Is.EqualTo(32));
            Assert.That(db.WorldNpcTemplates["smith"].activity, Is.EqualTo("forge"));
            Assert.That(db.WorldAdapters["fantasy_ember"].starter_content.default_player_class, Is.EqualTo("warrior"));
        }
    }
}
