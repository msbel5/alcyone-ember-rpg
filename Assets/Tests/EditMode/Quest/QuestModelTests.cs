using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Quest
{
    public sealed class QuestModelTests
    {
        private static readonly ActorId SubjectActor = new ActorId(17UL);

        [Test]
        public void InventoryHasItemTagCondition_IsMetExactlyAtRequiredCount()
        {
            var world = CreateWorld();
            var state = new QuestState(1, world.Time);
            var condition = new InventoryHasItemTagCondition("iron_ingot", 2);
            var view = new QuestWorldView(world);

            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ingot", "Iron Ingot", 1));
            Assert.That(condition.IsMet(in view, state), Is.False);

            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(2UL), "iron_ingot", "Iron Ingot", 1));
            Assert.That(condition.IsMet(in view, state), Is.True);
        }

        [Test]
        public void QuestTask_TriggersOnce_AndAppliesActionsInOrder()
        {
            var world = CreateWorld();
            var state = new QuestState(1, world.Time);
            var context = new QuestMutationContext(world, state, SubjectActor);
            var task = new QuestTask(
                new InventoryHasItemTagCondition("iron_ingot", 1),
                new IQuestAction[]
                {
                    new AppendQuestEventAction(WorldEventKind.QuestTaskTriggered, "quest:first"),
                    new GrantItemAction("quest_token", 1),
                    new AppendQuestEventAction(WorldEventKind.QuestTaskTriggered, "quest:second"),
                });

            world.PlayerInventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ingot", "Iron Ingot", 1));
            var view = new QuestWorldView(world);

            Assert.That(task.TryTrigger(0, in view, context), Is.True);
            Assert.That(task.TryTrigger(0, in view, context), Is.False);
            Assert.That(task.Triggered, Is.True);
            Assert.That(state.IsTaskTriggered(0), Is.True);
            Assert.That(Quantity(world.PlayerInventory, "quest_token"), Is.EqualTo(1));
            Assert.That(world.Events.Events.Select(evt => evt.Reason).ToArray(), Is.EqualTo(new[] { "quest:first", "quest:second" }));
        }

        [Test]
        public void CompleteQuestAction_SetsQuestState_AndAppendQuestEventAction_AppendsQuestCompletedEvent()
        {
            var world = CreateWorld();
            var state = new QuestState(1, world.Time);
            var context = new QuestMutationContext(world, state, SubjectActor);

            new CompleteQuestAction(success: true).Apply(context);
            new AppendQuestEventAction(WorldEventKind.QuestCompleted, "quest_completed:test").Apply(context);

            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.IsSuccess, Is.True);
            Assert.That(world.Events.Count, Is.EqualTo(1));
            Assert.That(world.Events.Events[0].Kind, Is.EqualTo(WorldEventKind.QuestCompleted));
            Assert.That(world.Events.Events[0].Reason, Is.EqualTo("quest_completed:test"));
        }

        [Test]
        public void SameInputs_ProduceSameQuestStateTransitions()
        {
            var first = RunDeterministicQuest();
            var second = RunDeterministicQuest();

            Assert.That(first.TaskTriggered, Is.EqualTo(second.TaskTriggered));
            Assert.That(first.State.IsComplete, Is.EqualTo(second.State.IsComplete));
            Assert.That(first.State.IsSuccess, Is.EqualTo(second.State.IsSuccess));
            Assert.That(first.State.TriggeredTasks, Is.EqualTo(second.State.TriggeredTasks));
            Assert.That(first.World.Events.Events.Select(evt => evt.Kind).ToArray(), Is.EqualTo(second.World.Events.Events.Select(evt => evt.Kind).ToArray()));
            Assert.That(first.World.Events.Events.Select(evt => evt.Reason).ToArray(), Is.EqualTo(second.World.Events.Events.Select(evt => evt.Reason).ToArray()));
            Assert.That(Quantity(first.World.PlayerInventory, "quest_reward"), Is.EqualTo(Quantity(second.World.PlayerInventory, "quest_reward")));
        }

        private static (bool TaskTriggered, QuestState State, WorldState World) RunDeterministicQuest()
        {
            var world = CreateWorld();
            world.Actors.Add(CreateActor(SubjectActor, alive: false));

            var state = new QuestState(1, world.Time);
            var context = new QuestMutationContext(world, state, SubjectActor);
            var task = new QuestTask(
                new ActorDeadCondition(SubjectActor),
                new IQuestAction[]
                {
                    new GrantItemAction("quest_reward", 1),
                    new CompleteQuestAction(success: true),
                    new AppendQuestEventAction(WorldEventKind.QuestCompleted, "quest_completed:deterministic"),
                });

            var view = new QuestWorldView(world);
            var triggered = task.TryTrigger(0, in view, context);
            return (triggered, state, world);
        }

        private static WorldState CreateWorld()
        {
            return new WorldState
            {
                Time = new GameTime(480),
                PlayerInventory = new InventoryState(8),
            };
        }

        private static ActorRecord CreateActor(ActorId actorId, bool alive)
        {
            return new ActorRecord(
                actorId,
                alive ? "Guard" : "Corpse",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(alive ? 10 : 0, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 30,
                dodge: 10,
                armor: 0,
                baseDamage: 2);
        }

        private static int Quantity(InventoryState inventory, string templateId)
        {
            return inventory.Items
                .Where(item => item.TemplateId == templateId)
                .Sum(item => item.Quantity);
        }
    }
}
