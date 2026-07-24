using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W32 DOC6 T3: hunger drops ONLY when ConsumeFood completes. Arrival is a phase change;
    /// the cost (the unit) and the benefit (hunger) change hands in ONE operation on the
    /// completion tick — the W20 "reaching the table IS the meal" era is over.
    /// </summary>
    public sealed class EatHungerAtCompletionTests
    {
        [Test]
        public void HungerDropsExactlyOnce_OnTheMealTick_AndConsumeReallyLasts()
        {
            var world = EatSliceWorld.Build();
            world.Actors.Add(EatSliceWorld.Hungry(7, 5, 7)); // seat ring: short MoveToFood, 1-tick Take
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            ActorRecord A() => world.Actors.Get(new ActorId(7));
            var trace = new List<(int tick, long minutes, int hunger, ActorActionType action)>();
            for (var tick = 1; tick <= 3 * ConsumeFoodAdvancer.ConsumeDurationTicks + 20; tick++)
            {
                composer.Advance(world, tick);
                trace.Add((tick, world.Time.TotalMinutes, A().Needs.Hunger.Value, A().ActionState.CurrentAction));
            }

            int drops = 0, dropIndex = -1;
            for (var i = 1; i < trace.Count; i++)
                if (trace[i].hunger < trace[i - 1].hunger) { drops++; if (dropIndex < 0) dropIndex = i; }
            Assert.That(drops, Is.EqualTo(1), "hunger drops EXACTLY once — no mid-phase leak");

            var meal = world.Events.Events.Single(e => e.Kind == WorldEventKind.ActionCompleted);
            Assert.That(trace[dropIndex].minutes, Is.EqualTo(meal.Tick.TotalMinutes),
                "the drop tick == the MealConsumed (ActionCompleted) tick");
            Assert.That(trace.Take(dropIndex).All(s => s.hunger >= 80), Is.True,
                "through MoveToFood/TakeFood/mid-Consume hunger may only RISE (NeedsSystem)");
            Assert.That(trace[dropIndex].hunger, Is.EqualTo(NeedConsumptionSystem.MealHungerFloor),
                "eat-to-satiation is preserved");
            Assert.That(trace.Count(s => s.action == ActorActionType.ConsumeFood),
                Is.GreaterThanOrEqualTo(ConsumeFoodAdvancer.ConsumeDurationTicks),
                "the meal is not a one-tick teleport — ConsumeFood really LASTS");
        }
    }
}
