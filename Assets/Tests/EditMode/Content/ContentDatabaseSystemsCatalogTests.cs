using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Content
{
    public sealed class ContentDatabaseSystemsCatalogTests
    {
        [Test]
        public void LoadFromStreamingAssets_ExposesSystemsCatalogs()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.EconomyConfig.commodities.Count, Is.GreaterThan(0));
            Assert.That(db.ColonyConfig.needs.Count, Is.GreaterThan(0));
            Assert.That(db.Caravans.Count, Is.GreaterThan(0));
            Assert.That(db.DialogDefinitions.Count, Is.GreaterThan(0));
            Assert.That(db.HistoryTables.present_year, Is.GreaterThan(0));
            Assert.That(db.Institutions.town_institutions.Count, Is.GreaterThan(0));
            Assert.That(db.Schedules.default_schedules.Count, Is.GreaterThan(0));
            Assert.That(db.SocialRules.default_npc_attitude.Count, Is.GreaterThan(0));
            Assert.That(db.InteractionRules.Count, Is.GreaterThan(0));
            Assert.That(db.ConsequenceRules.Count, Is.GreaterThan(0));
            Assert.That(db.LootTables.monster_tables.Count, Is.GreaterThan(0));
            Assert.That(db.QualityTiers.Count, Is.GreaterThan(0));
            Assert.That(db.InventoryLayouts.default_equipment_slots.Count, Is.GreaterThan(0));
            Assert.That(db.Progression.class_abilities.Count, Is.GreaterThan(0));
            Assert.That(db.CharacterCreation.question_groups.Count, Is.GreaterThan(0));
            Assert.That(db.NameBanks.Count, Is.GreaterThan(0));
            Assert.That(db.QuestConfig.reward_scales.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoadFromStreamingAssets_DeserializesSystemsSamples()
        {
            var db = ContentDatabaseTestContext.Load();

            Assert.That(db.EconomyConfig.commodities[0].item_id, Is.EqualTo("wheat_flour"));
            Assert.That(db.ColonyConfig.needs["eat"].label, Is.EqualTo("Hunger"));
            Assert.That(db.Caravans["iron_caravan"].origin, Is.EqualTo("ironhold_mines"));
            Assert.That(db.DialogDefinitions["guard_gate"].dialog_id, Is.EqualTo("guard_gate"));
            Assert.That(db.HistoryTables.present_year, Is.EqualTo(1000));
            Assert.That(db.Institutions.town_institutions["harbor_town"]["mayor"].title, Is.EqualTo("Mayor of Harbor Town"));
            Assert.That(db.Schedules.default_schedules["merchant"]["morning"], Is.EqualTo("shop"));
            Assert.That(db.SocialRules.default_npc_attitude["guard"], Is.EqualTo("indifferent"));
            Assert.That(db.InteractionRules[0].interaction_type, Is.EqualTo("TALK"));
            Assert.That(db.ConsequenceRules["merchant_killed_price_rise"].rule_id, Is.EqualTo("merchant_killed_price_rise"));
            Assert.That(db.LootTables.monster_tables["wolf"].gold_range[0], Is.EqualTo(2));
            Assert.That(db.QualityTiers["0"].label, Is.EqualTo("Poor"));
            Assert.That(db.InventoryLayouts.default_equipment_slots[0], Is.EqualTo("weapon"));
            Assert.That(db.Progression.class_abilities["warrior"][0].name, Is.EqualTo("Combat Stance"));
            Assert.That(db.CharacterCreation.default_class, Is.EqualTo("warrior"));
            Assert.That(db.NameBanks["human"].male_first[0], Is.EqualTo("Aldric"));
            Assert.That(db.QuestConfig.emergent_shortages[0].item_id, Is.EqualTo("elven_bread"));
        }
    }
}
