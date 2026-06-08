#if UNITY_EDITOR
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class PlayableLoopCraftQuestTests
    {
        [Test]
        public void CraftingFirstIngot_CompletesStartingQuestImmediately()
        {
            var world = new WorldFactory().Create(roomSeed: 17);
            world.Quests.Add(QuestCatalog.ForgeIronIngotId, new QuestState(1, world.Time));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(90UL), "iron_ore", "Iron Ore", 2));
            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(91UL), "fuel", "Fuel", 1));
            var adapter = new DomainSimulationAdapter(world);

            var result = ((ICraftingCommandSink)adapter).ExecuteCraft("1001");
            var chapters = ((IJournalSource)adapter).GetChapters();

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(chapters.Count, Is.EqualTo(1));
            Assert.That(chapters[0].Entries[0].Status, Is.EqualTo(JournalEntryStatus.Completed));
            Assert.That(chapters[0].Entries[0].Body, Does.Contain("has been met"));
        }
    }
}
#endif
