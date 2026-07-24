using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
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
    /// W32 DOC6 T1: an actor cannot eat from afar. Distance is a precondition the OPERATION
    /// itself validates — no matter how the phase was reached (the setup below forces phases
    /// aggressively, bypassing the chain), a remote Take/Consume is PHYSICALLY refused.
    /// </summary>
    public sealed class EatAtDistanceTests
    {
        [Test]
        public void RemoteTake_IsRefused_NothingMoves()
        {
            var world = EatSliceWorld.Build(wheat: 1);
            var far = EatSliceWorld.Hungry(7, 50, 50); // Chebyshev 45 >> EatReachCells(2)
            world.Actors.Add(far);
            Assert.That(world.Reservations.TryReserve(1UL, "wheat", 7UL,
                untilMinutes: 999, pileCount: 1, out var rid), Is.True);
            // Aggressive setup: force TakeFood without ever walking (bypasses the phase order).
            far.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.TakeFood, new SiteId(1), ItemId.Empty,
                new ReservationId(rid), startedAtMinutes: 61, ActionInterruptPolicy.Interruptible));

            new TakeFoodAdvancer(new ActionLogManager()).Advance(world, far, new GameTime(61));

            Assert.That(far.ActionState.Phase, Is.EqualTo(ActionPhase.Failed), "remote TAKE is REFUSED");
            Assert.That(far.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable),
                "the op refusal leaves a reason (too_far)");
            Assert.That(far.Needs.Hunger.Value, Is.EqualTo(80), "hunger did not move");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(1), "stock did not move");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted), Is.False,
                "no meal resolves from afar");
        }

        [Test]
        public void RemoteConsume_IsRefused_TheCarriedUnitGoesHome()
        {
            var world = EatSliceWorld.Build(wheat: 1);
            var far = EatSliceWorld.Hungry(7, 50, 50);
            world.Actors.Add(far);
            Assert.That(world.Reservations.TryReserve(1UL, "wheat", 7UL,
                untilMinutes: 999, pileCount: 1, out var rid), Is.True);
            world.Stockpiles[0].Remove("wheat", 1); // the unit is "in hand" (take semantics)
            far.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.ConsumeFood, new SiteId(1), ItemId.Empty,
                new ReservationId(rid), startedAtMinutes: 61, ActionInterruptPolicy.Interruptible));

            new ConsumeFoodAdvancer(new ActionLogManager()).Advance(world, far, new GameTime(61));

            Assert.That(far.ActionState.Phase, Is.EqualTo(ActionPhase.Failed), "remote CONSUME is REFUSED");
            Assert.That(far.ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Unreachable));
            Assert.That(far.Needs.Hunger.Value, Is.EqualTo(80), "hunger did not move");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(1),
                "MATTER CONSERVATION: the carried unit returns to the pile — no loss, no dup");
            Assert.That(world.Reservations.Rows, Is.Empty, "the refused claim is released");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted), Is.False);
        }

        [Test]
        public void Composer_TwoHours_EveryMealResolvesWithinEatReach()
        {
            // Integration half: under the full composer no MealConsumed-class outcome may ever
            // land while its eater stands outside EatReachCells of the pile's site centre.
            var world = EatSliceWorld.Build();
            world.Actors.Add(EatSliceWorld.Hungry(1, 4, 4));   // near
            world.Actors.Add(EatSliceWorld.Hungry(2, 30, 30)); // mid walk
            world.Actors.Add(EatSliceWorld.Hungry(3, 50, 50)); // long walk
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            NeedConsumptionSystem.TryGetSiteCentre(world, new SiteId(1), out var centre);
            int seen = 0, checkedEvents = 0;
            for (var tick = 1; tick <= 120; tick++)
            {
                composer.Advance(world, tick);
                foreach (var e in world.Events.Events.Skip(checkedEvents).ToList())
                {
                    checkedEvents++;
                    if (e.Kind != WorldEventKind.ActionCompleted) continue;
                    seen++;
                    var eater = world.Actors.Get(e.ActorId);
                    long reach = System.Math.Max(
                        System.Math.Abs(eater.Position.X - centre.X),
                        System.Math.Abs(eater.Position.Y - centre.Y));
                    Assert.That(reach, Is.LessThanOrEqualTo(NeedConsumptionSystem.EatReachCells),
                        $"a meal resolved at reach {reach} on tick {tick} — eating happened at distance");
                }
            }
            Assert.That(seen, Is.GreaterThanOrEqualTo(3), "vacuous guard: all three diners must actually eat");
        }
    }
}
