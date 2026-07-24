using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W32 DOC6 T5: interruption at EVERY phase frees the reservation and conserves matter —
    /// the unit neither vanishes nor duplicates. The slice's one interruption gate is the
    /// pull-based pursuit probe at each advancement step (being hunted outranks lunch); a
    /// half-eaten unit returns to the pile whole (slice simplification, by design).
    /// </summary>
    public sealed class EatInterruptionConservationTests
    {
        // The observable interruption points of the chain (TakeFood is a 1-tick link, so its
        // interruption point is the arrival handover the probe preempts on the take tick).
        [TestCase(ActorActionType.MoveToFood)]
        [TestCase(ActorActionType.TakeFood)]
        [TestCase(ActorActionType.ConsumeFood)]
        public void Interrupt_AtPhase_ConservesFoodAndFreesReservation(ActorActionType at)
        {
            var world = EatSliceWorld.Build(wheat: 1);
            world.Actors.Add(EatSliceWorld.Hungry(7, 12, 12));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(7));

            int tick = 0;
            bool AtTarget()
            {
                var s = A().ActionState;
                if (at == ActorActionType.TakeFood) // interrupted DURING the take step's tick
                    return s.CurrentAction == ActorActionType.MoveToFood && s.Phase == ActionPhase.Succeeded;
                return s.CurrentAction == at && s.Phase == ActionPhase.Running;
            }
            while (!AtTarget() && tick < 50) composer.Advance(world, ++tick);
            Assert.That(AtTarget(), Is.True, "deterministic run-up never reached the phase under test");

            // The design's single interruption gate: an armed chase targeting the diner.
            world.GuardPursuits.Add(new PursuitRecord
            { GuardId = 99UL, TargetId = 7UL, UntilMinutes = world.Time.TotalMinutes + 50 });
            composer.Advance(world, ++tick); // the probe fires before the step

            Assert.That(A().ActionState.Phase, Is.EqualTo(ActionPhase.Failed), "the action fell");
            Assert.That(A().ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Interrupted),
                "the reason travels on the state for the handover tick");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionFailed
                && e.Reason != null && e.Reason.Contains("InterruptPreempted")), Is.True,
                "the reason reaches the story log");

            composer.Advance(world, ++tick); // one more tick: the cleanup settles (Failed -> Idle)
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.EqualTo(1),
                "MATTER CONSERVATION: no dup, no loss — the unit is back in the pile");
            Assert.That(world.Reservations.Rows, Is.Empty, "the reservation is freed");
            Assert.That(A().ActionState.CurrentAction, Is.EqualTo(ActorActionType.None), "the action dropped");
            Assert.That(A().Needs.Hunger.Value, Is.EqualTo(80), "a half meal feeds nobody");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted), Is.False);

            // PROOF of the release: a second actor can reserve the same last unit RIGHT NOW.
            world.Actors.Add(EatSliceWorld.Hungry(8, 5, 6));
            Assert.That(world.Reservations.TryReserve(1UL, "wheat", 8UL,
                untilMinutes: world.Time.TotalMinutes + 99, pileCount: 1, out _), Is.True,
                "the unit belongs to the world again");
        }
    }
}
