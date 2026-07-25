using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// guards-eat (W33-05 fix 4, the B09 remainder): the role gate already ADMITS guards to
    /// EatIntent — these tests PIN that grant (removing guards from the set is now a red
    /// build, not a silent re-starve) and pin the new carve-out: a live pursuit outranks
    /// lunch, mirroring the quarry-side probe, with the SAME expiry predicate (&lt;=).
    /// </summary>
    public sealed class GuardEatStoryTests
    {
        [Test]
        public void GuardEatsOffWatch()
        {
            var world = EatSliceWorld.Build();
            world.Actors.Add(Guard(7, 5, 7, hunger: 80)); // >= HungerEatThreshold (55), no pursuits
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            for (var tick = 1; tick <= 200
                && !world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted); tick++)
                composer.Advance(world, tick);

            var meal = world.Events.Events.Single(e => e.Kind == WorldEventKind.ActionCompleted);
            Assert.That(meal.ActorId.Value, Is.EqualTo(7UL), "the guard himself completed the meal");
            Assert.That(ActionTrace.Of(world), Does.Contain("ConsumeFood/Running->ConsumeFood/Succeeded"),
                "the guard ran the REAL eat chain — not a schedule-side shortcut");
            Assert.That(world.Actors.Get(new ActorId(7)).Needs.Hunger.Value,
                Is.EqualTo(NeedConsumptionSystem.MealHungerFloor),
                "hunger dropped at the meal commit (the civilian pin, now for the watch)");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(9), "the transfer was physical");
        }

        [Test]
        public void PursuitOutranksLunch()
        {
            var world = EatSliceWorld.Build();
            var guard = Guard(7, 5, 7, hunger: 80);
            world.Actors.Add(guard);
            world.Actors.Add(Bystander(9, 9, 7)); // the quarry: fed, alive, within 40 cells
            world.GuardPursuits.Add(new PursuitRecord { GuardId = 7, TargetId = 9, UntilMinutes = 100000 });
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            composer.Advance(world, 1);

            Assert.That(guard.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None),
                "a hungry mid-chase guard gets NO EatIntent — lunch may not starve justice");
            Assert.That(Chebyshev(guard.Position, world.Actors.Get(new ActorId(9)).Position),
                Is.EqualTo(3), // was 4: the schedule (which still owns his legs) stepped the chase
                "the SAME tick still steps him toward the quarry");

            for (var tick = 2; tick <= 10; tick++)
                composer.Advance(world, tick);
            Assert.That(guard.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None),
                "the chase stays lunch-proof while the pursuit row is live");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted), Is.False);
        }

        [Test]
        public void LunchAfterTheChase()
        {
            var world = EatSliceWorld.Build(); // Time starts at minute 60; core.time advances FIRST
            var guard = Guard(7, 5, 7, hunger: 80);
            world.Actors.Add(guard);
            world.Actors.Add(Bystander(9, 9, 7));
            // UntilMinutes == the FIRST decide stamp (61): live that tick, expired the next —
            // pins the expiry predicate to <=, exactly ActionAdvancer.IsPursuitQuarry's.
            world.GuardPursuits.Add(new PursuitRecord { GuardId = 7, TargetId = 9, UntilMinutes = 61 });
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            composer.Advance(world, 1); // stamp 61: 61 <= 61 -> still live
            Assert.That(guard.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None),
                "on the boundary minute the pursuit still outranks lunch (<=, not <)");

            composer.Advance(world, 2); // stamp 62: expired -> the next Decide grants EatIntent
            Assert.That(guard.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.Eat),
                "the chase over, the watch finally eats");
            Assert.That(guard.ActionState.CurrentAction, Is.EqualTo(ActorActionType.MoveToFood));
        }

        private static ActorRecord Guard(ulong id, int x, int y, int hunger)
        {
            var actor = new ActorRecord(
                new ActorId(id), "Watch" + id, ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
            actor.ApplyNeeds(actor.Needs.WithHunger(new NeedValue(hunger)));
            return actor;
        }

        /// <summary>A fed civilian quarry: never decides an action, never moves off its home cell.</summary>
        private static ActorRecord Bystander(ulong id, int x, int y)
        {
            return new ActorRecord(
                new ActorId(id), "Bystander" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
        }

        private static int Chebyshev(GridPosition a, GridPosition b)
        {
            return System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));
        }
    }
}
