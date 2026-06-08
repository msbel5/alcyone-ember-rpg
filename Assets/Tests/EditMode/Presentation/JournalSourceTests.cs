#if UNITY_EDITOR
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class JournalSourceTests
    {
        [Test]
        public void UnavailableAdapter_DoesNotFabricateJournalChapter()
        {
            var adapter = new UnavailableSimulationAdapter();

            var chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(chapters, Is.Empty);
        }

        [Test]
        public void DomainAdapter_ProjectsSeededQuestIntoJournal()
        {
            var world = new WorldFactory().Create(roomSeed: 1);
            world.Quests.Add(QuestCatalog.ForgeIronIngotId, new QuestState(1, new GameTime(90)));
            var adapter = new DomainSimulationAdapter(world);

            var chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(chapters, Has.Count.EqualTo(1));
            Assert.That(chapters[0].Entries, Has.Count.EqualTo(1));
            Assert.That(chapters[0].Entries[0].Title, Is.EqualTo("Forge an Iron Ingot"));
            StringAssert.Contains("Year 1", chapters[0].Entries[0].DateLabel);
            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Active));
        }

        [Test]
        public void DomainAdapter_UsesStartingSettlementHookWhenWorldIsSeeded()
        {
            var world = new WorldFactory().Create(roomSeed: 1);
            var adapter = new DomainSimulationAdapter(world);

            adapter.SeedWorld("grim", "survival", "crossroads", 7u);
            world.Quests.Add(QuestCatalog.ForgeIronIngotId, new QuestState(1, world.Time));
            var chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(chapters, Is.Not.Empty);
            Assert.That(chapters[0].Entries, Is.Not.Empty);
            StringAssert.Contains(adapter.StartingSettlementName, chapters[0].Entries[0].Body);
        }
    }
}
#endif
