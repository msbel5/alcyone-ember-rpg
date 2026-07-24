using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Living.Actions;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>
    /// W32 EAT: the phase-machine successors of the retired eat-on-arrival pins. The W20/P0
    /// contract "reaching the table IS the meal" is deliberately REVERSED (historical note,
    /// docs/ruh/w32/03-action-advancement.md §13): arrival is a transition, the pile drops at
    /// TakeFood, hunger drops only at the ConsumeFood commit — and the meal_eaten event line
    /// stays verbatim so every meal counter keeps reading it.
    /// </summary>
    public sealed class EatActionStoryTests
    {
        private static WorldState Build(int wheat = 10)
        {
            var world = new WorldState();
            world.EnsureInvariants();
            world.Time = new GameTime(60);
            world.Sites.Add(new SiteRecord(new SiteId(1), SiteKind.Settlement, "Town",
                new GridPosition(0, 0), new GridPosition(10, 10))); // centre (5,5)
            var pile = new StockpileComponent(new SiteId(1));
            pile.Add("wheat", wheat);
            world.Stockpiles.Add(pile);
            return world;
        }

        private static ActorRecord Hungry(ulong id, int x, int y)
        {
            var actor = new ActorRecord(
                new ActorId(id), "Diner" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
            actor.ApplyNeeds(actor.Needs.WithHunger(new NeedValue(80)));
            return actor;
        }

        private static ActionLifecycleSystem Lifecycle() => new ActionLifecycleSystem(new ActionLogManager());

        // One decide+advance pass per game-minute — the composer's PerTick 18/22 pair in miniature.
        private static void Pump(WorldState world, ActionLifecycleSystem lifecycle, int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                world.Time = world.Time.AddMinutes(1);
                lifecycle.Decide(world, world.Time);
                lifecycle.Advance(world, world.Time);
            }
        }

        private static int Meals(WorldState world) =>
            world.Events.Events.Count(e => e.Reason != null && e.Reason.StartsWith("meal_eaten"));

        [Test]
        public void HungryCivilian_WalksTakesAndConsumes_HungerDropsOnlyAtCompletion()
        {
            var world = Build();
            var diner = Hungry(7, 3, 6); // id 7 -> seat ordinal 7 -> ring cell (3,6): already seated
            world.Actors.Add(diner);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle); // decide + first step: already on the seat -> Arrived
            Assert.That(diner.ActionState.CurrentAction, Is.EqualTo(ActorActionType.MoveToFood));
            Assert.That(diner.ActionState.Phase, Is.EqualTo(ActionPhase.Succeeded));
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(10), "arrival is NOT the meal");
            Assert.That(diner.Needs.Hunger.Value, Is.EqualTo(80), "arrival feeds nobody");

            Pump(world, lifecycle); // TakeFood: the pile drops, the hand fills
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(9), "take decrements exactly one unit");
            Assert.That(diner.Needs.Hunger.Value, Is.EqualTo(80), "carrying is not eating");

            Pump(world, lifecycle, 3); // ConsumeFood 1/3, 2/3, 3/3 -> atomic commit
            Assert.That(diner.Needs.Hunger.Value, Is.EqualTo(NeedConsumptionSystem.MealHungerFloor),
                "hunger drops to the meal floor ONLY at completion");
            Assert.That(Meals(world), Is.EqualTo(1), "the meal_eaten line survives verbatim");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted), Is.True,
                "the terminal outcome reaches the story log");
            Assert.That(world.Reservations.Rows, Is.Empty, "the claim closes with the commit");
            Assert.That(world.ActionLog.TotalPushed, Is.GreaterThanOrEqualTo(5),
                "every phase boundary passed through the ring");

            Pump(world, lifecycle); // terminal handover
            Assert.That(diner.ActionState.IsIdle, Is.True, "a finished diner returns to the schedule");
        }

        [Test]
        public void RemoteEating_IsImpossible_TheWalkStillMatters()
        {
            var world = Build();
            var far = Hungry(8, 50, 50); // Chebyshev ~45 to the larder
            world.Actors.Add(far);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle, 10);

            Assert.That(Meals(world), Is.Zero, "no meal resolves before the walk completes");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(10), "the pile is untouched from afar");
            Assert.That(far.ActionState.CurrentAction, Is.EqualTo(ActorActionType.MoveToFood),
                "the hungry actor is EN ROUTE, not fed");
        }

        [Test]
        public void LastUnit_TwoHungryActors_OneReservationOneRefusal()
        {
            var world = Build(wheat: 1);
            world.Actors.Add(Hungry(1, 4, 4)); // store insertion order decides, not a seed
            world.Actors.Add(Hungry(2, 4, 5));
            var lifecycle = Lifecycle();

            Pump(world, lifecycle);
            Assert.That(world.Reservations.Rows.Count, Is.EqualTo(1), "one unit -> one claim");
            Assert.That(world.Reservations.Rows[0].ActorId, Is.EqualTo(1UL),
                "the winner is DETERMINISTIC: actor-store insertion order");
            Assert.That(world.Actors.Get(new ActorId(2)).ActionState.IsIdle, Is.True,
                "the loser gets no EatAction and falls through to the schedule");

            Pump(world, lifecycle, 30); // run the episode out
            Assert.That(Meals(world), Is.EqualTo(1), "one unit feeds exactly one mouth");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.Zero);
        }

        [Test]
        public void PileDrained_MidRoute_FailsSourceDrained_AndReplans()
        {
            var world = Build(wheat: 1);
            var walker = Hungry(3, 20, 20);
            world.Actors.Add(walker);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle, 2); // reservation held, walking
            Assert.That(walker.ActionState.CurrentAction, Is.EqualTo(ActorActionType.MoveToFood));
            world.Stockpiles[0].Remove("wheat", 1); // vermin-class external drain

            Pump(world, lifecycle);
            Assert.That(walker.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(walker.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.SourceDrained));
            Assert.That(world.Reservations.Rows, Is.Empty, "the failed claim is released ONCE");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionFailed), Is.True);

            world.Stockpiles[0].Add("wheat", 1); // the larder recovers
            Pump(world, lifecycle, 2);           // Failed -> Idle, then a fresh decision
            Assert.That(walker.ActionState.CurrentAction, Is.EqualTo(ActorActionType.MoveToFood),
                "the next decision replans toward the recovered larder");
        }

        [Test]
        public void InterruptedConsume_ConservesTheUnit_AndFreesTheClaim()
        {
            var world = Build(wheat: 1);
            var diner = Hungry(7, 3, 6); // seated from tick one
            world.Actors.Add(diner);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle, 3); // arrive, take, first chew
            Assert.That(diner.ActionState.CurrentAction, Is.EqualTo(ActorActionType.ConsumeFood));
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.Zero, "the unit is in hand");

            world.GuardPursuits.Add(new PursuitRecord
            { GuardId = 99UL, TargetId = 7UL, UntilMinutes = world.Time.TotalMinutes + 50 });
            Pump(world, lifecycle); // probe fires before the chew

            Assert.That(diner.ActionState.Phase, Is.EqualTo(ActionPhase.Failed));
            Assert.That(diner.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Interrupted));
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(1),
                "MATTER CONSERVATION: the carried unit returns to the pile — no dup, no loss");
            Assert.That(world.Reservations.Rows, Is.Empty, "the claim is freed");
            Assert.That(diner.Needs.Hunger.Value, Is.EqualTo(80), "a half-meal feeds nobody");
            Assert.That(Meals(world), Is.Zero);
        }

        [Test]
        public void ExpiredReservation_IsSweptDeterministically_AndFreesTheUnit()
        {
            var world = Build(wheat: 1);
            world.Reservations.TryReserve(1UL, "wheat", actorId: 999UL,
                untilMinutes: world.Time.TotalMinutes + 1, pileCount: 1, out _); // leaked row (dead actor class)
            world.Actors.Add(Hungry(4, 4, 4));
            var lifecycle = Lifecycle();

            Pump(world, lifecycle); // effective stock 0: the hungry actor is refused
            Assert.That(world.Actors.Get(new ActorId(4)).ActionState.IsIdle, Is.True);

            Pump(world, lifecycle, 2); // TTL passes -> sweep frees the unit -> decision claims it
            Assert.That(world.Reservations.Rows.Count, Is.EqualTo(1));
            Assert.That(world.Reservations.Rows[0].ActorId, Is.EqualTo(4UL),
                "the swept unit is the world's again");
        }

        [Test]
        public void Decision_SkipsActorsWithRunningAction_TheCommitmentHolds()
        {
            var world = Build();
            var walker = Hungry(5, 20, 20);
            world.Actors.Add(walker);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle);
            var reservation = walker.ActionState.ReservationId;
            var started = walker.ActionState.StartedAtMinutes;

            Pump(world, lifecycle, 5);
            Assert.That(walker.ActionState.ReservationId, Is.EqualTo(reservation),
                "no re-decision while the action runs — the episode keeps its identity");
            Assert.That(walker.ActionState.StartedAtMinutes, Is.EqualTo(started));
        }

        [Test]
        public void Decision_PicksTheNearestLarder_FirstWinsOnTies()
        {
            var world = Build();
            world.Sites.Add(new SiteRecord(new SiteId(2), SiteKind.Settlement, "Yonder",
                new GridPosition(40, 0), new GridPosition(50, 10))); // centre (45,5)
            var farPile = new StockpileComponent(new SiteId(2));
            farPile.Add("wheat", 10);
            world.Stockpiles.Add(farPile);
            world.Actors.Add(Hungry(6, 44, 6)); // beside site 2's centre
            var lifecycle = Lifecycle();

            Pump(world, lifecycle);
            Assert.That(world.Reservations.Rows.Single().SiteId, Is.EqualTo(2UL),
                "the claim lands on the NEAREST larder, not the globally-first pile");
        }
    }
}
