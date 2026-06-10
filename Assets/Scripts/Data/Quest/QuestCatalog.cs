using System;
using System.Collections.Generic;
using EmberCrpg.Data.Recipes;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Data.Quests
{
    /// <summary>Static catalog of authored quest definitions in stable id order.</summary>
    public static class QuestCatalog
    {
        public static readonly QuestId ForgeIronIngotId = new QuestId(2001UL);

        public static QuestDefinition ForgeIronIngot()
        {
            return new QuestDefinition(
                ForgeIronIngotId,
                "Forge an Iron Ingot",
                oneTime: true,
                new QuestResourceBinding(Array.Empty<KeyValuePair<string, QuestResourceValue>>()),
                new[]
                {
                    new QuestTask(
                        new AllQuestCondition(new IQuestCondition[]
                        {
                            new InventoryHasItemTagCondition("iron_ingot", 1),
                            new WorldEventOccurredCondition(
                                WorldEventKind.RecipeCompleted,
                                "recipe_completed:" + ProductionRecipeRegistry.SmeltIronIngotId.Value,
                                atOrAfterQuestStart: false),
                        }),
                        new IQuestAction[]
                        {
                            new AppendQuestEventAction(WorldEventKind.QuestTaskTriggered, "quest_task_triggered:forge_iron_ingot"),
                        }),
                },
                completionTaskIndex: 0);
        }

        public static IReadOnlyList<QuestDefinition> AllQuests()
        {
            return new[] { ForgeIronIngot() };
        }

        public static QuestDefinition Resolve(QuestId id)
        {
            if (id == ForgeIronIngotId)
                return ForgeIronIngot();

            throw new KeyNotFoundException($"QuestCatalog has no quest for {id}.");
        }
    }
}
