using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

// Design note:
// RecipeEventLogTests pins the player-visible part of the first RecipeSystem
// slice: completing SmeltIronIngot appends an ordered WorldEventLog entry with
// a causal ReasonTrace. This is the product-visible proof required by
// docs/agent-rules-v2.md rule 1.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies RecipeSystem world-event emission.</summary>
    public sealed class RecipeEventLogTests
    {
        [Test]
        public void CompletingRecipe_AppendsOrderedRecipeCompletedEventWithReasonTrace()
        {
            var site = new SiteId(77UL);
            var position = new GridPosition(4, 5);
            var actor = new ActorId(12UL);
            var recipe = new RecipeDef(
                new RecipeId(1001UL),
                "furnace",
                "smelting",
                40,
                new[] { new RecipeIngredient("iron_ore", 2), new RecipeIngredient("fuel", 1) },
                new[] { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) });
            var worksites = new WorksiteStore();
            worksites.Add(new WorksiteRecord(site, position, WorksiteKind.Furnace, isActive: true));
            var inventory = new InventoryState(8);
            inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ore", "Iron Ore", 2));
            inventory.TryAdd(new InventoryItem(new ItemId(2UL), "fuel", "Fuel", 1));
            var log = new WorldEventLog();
            var system = new RecipeSystem();

            Assert.That(system.TryStart(recipe, worksites, site, position, inventory, actor, out var order), Is.True);
            for (var i = 0; i < recipe.DurationTicks; i++)
                system.Tick(order, inventory, log, output => new InventoryItem(new ItemId(9001UL), output.ItemTag, "Iron Ingot", 1));

            Assert.That(log.Events, Has.Count.EqualTo(1));
            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.RecipeCompleted));
            Assert.That(evt.ActorId, Is.EqualTo(actor));
            Assert.That(evt.SiteId, Is.EqualTo(site));
            Assert.That(evt.Tick, Is.EqualTo(new GameTime(40L)));
            Assert.That(evt.Reason, Is.EqualTo("recipe_completed:1001"));
            Assert.That(evt.ReasonTrace, Is.Not.Null);
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[] { "recipe:1001", "worksite:furnace", "duration_ticks:40" }));
        }
    }
}
