using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Content
{
    public sealed class ContentDatabaseRuntimeCatalogTests
    {
        [Test]
        public void LoadFromStreamingAssets_ExposesRuntimeCatalogs()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.CampaignTemplates.Count, Is.GreaterThan(0));
            Assert.That(db.CampaignRuntime.arc_titles.Count, Is.GreaterThan(0));
            Assert.That(db.AdapterPrompts.Count, Is.GreaterThan(0));
            Assert.That(db.RuntimeConfig.llm.models.Count, Is.GreaterThan(0));
            Assert.That(db.UiPlan.jobs.Count, Is.GreaterThan(0));
            Assert.That(db.CampaignArcs.Count, Is.GreaterThan(0));
            Assert.That(db.LegacyNpcs.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoadFromStreamingAssets_DeserializesRuntimeSamples()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.CampaignTemplates["tutorial_forgotten_crypt"].id, Is.EqualTo("tutorial_forgotten_crypt"));
            Assert.That(db.CampaignRuntime.arc_titles[0], Is.EqualTo("Dark Forest"));
            Assert.That(db.AdapterPrompts["fantasy_ember"].seed_offset, Is.EqualTo(0));
            Assert.That(db.RuntimeConfig.llm.narration_mode_default, Is.EqualTo("prefer_live"));
            Assert.That(db.UiPlan.jobs[0].id, Is.EqualTo("instrument_rail_panel"));
            Assert.That(db.CampaignArcs["main_quest_campaign"].id, Is.EqualTo("main_quest_campaign"));
            Assert.That(db.CampaignArcs["tutorial_campaign"].acts[0].id, Is.EqualTo("act_1"));
            Assert.That(db.LegacyNpcs["bartender_rynna"].id, Is.EqualTo("bartender_rynna"));
        }
    }
}
