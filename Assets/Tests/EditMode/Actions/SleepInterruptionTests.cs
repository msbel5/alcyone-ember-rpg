using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W34 DOC4 S2: a hunted sleeper WAKES. The ActionAdvancer's pursuit probe fires at the
    /// top of Sleep's next Running tick — Failed(Interrupted), reservation released, banked
    /// recovery preserved (matter conservation for fatigue: half a night rests half a body,
    /// but the wakening is not a REFUND). The projection's verb is the ACTION verb during
    /// the terminal tick and null once handover clears — the label follows truth.
    /// </summary>
    public sealed class SleepInterruptionTests
    {
        [Test]
        public void PursuitAgainstASleeper_FailsSleepAsInterrupted_AndReleasesTheBed()
        {
            var world = SleepSliceWorld.Build();
            world.Actors.Add(SleepSliceWorld.Tired(9,
                SleepSliceWorld.BedRoom.X, SleepSliceWorld.BedRoom.Y)); // already home
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(9));

            // Run to the first Running Sleep tick.
            int tick = 0;
            while (tick < 4 * 60
                   && !(A().ActionState.CurrentAction == ActorActionType.Sleep
                        && A().ActionState.Phase == ActionPhase.Running))
                composer.Advance(world, ++tick);
            Assert.That(A().ActionState.CurrentAction, Is.EqualTo(ActorActionType.Sleep));
            Assert.That(A().ActionState.Phase, Is.EqualTo(ActionPhase.Running),
                "the horizon must actually reach a Running Sleep — otherwise the interrupt tests nothing");
            var bedded = A().Needs.Fatigue.Value;
            Assert.That(SleepSliceWorld.BedReservations(world, 9UL), Is.EqualTo(1),
                "the bed row lives while asleep");

            // Verb TRACKS TRUTH: while the actor is Sleep/Running the label is "sleeping".
            Assert.That(ActionVerbTable.Verb(A().ActionState.CurrentAction), Is.EqualTo("sleeping"));

            // The single interruption gate: an armed chase targeting the sleeper.
            world.GuardPursuits.Add(new PursuitRecord
            { GuardId = 99_999UL, TargetId = 9UL, UntilMinutes = world.Time.TotalMinutes + 100 });
            composer.Advance(world, ++tick); // the probe fires before the step

            Assert.That(A().ActionState.Phase, Is.EqualTo(ActionPhase.Failed), "the sleeper woke");
            Assert.That(A().ActionState.FailureReason, Is.EqualTo(ActionFailureReason.Interrupted),
                "the wake carries its reason — hunters outrank beds");
            Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionFailed
                && e.ActorId.Value == 9UL
                && e.Reason != null && e.Reason.Contains("InterruptPreempted")), Is.True,
                "the wake speaks itself in the story log");
            // Recovery banked so far is KEPT — a woken night is not a refund night.
            Assert.That(A().Needs.Fatigue.Value, Is.LessThanOrEqualTo(bedded),
                "banked recovery survives the wake — no fatigue refund on interrupt");

            composer.Advance(world, ++tick); // the cleanup settles (Failed -> Idle handover)
            Assert.That(SleepSliceWorld.BedReservations(world, 9UL), Is.EqualTo(0),
                "the bed row was released — the family cell is free for the next sleeper");
        }

        [Test]
        public void AfterWake_ProjectionLabelFollowsTheAction_AndTheHandoverClearsIt()
        {
            var world = SleepSliceWorld.Build();
            world.Actors.Add(SleepSliceWorld.Tired(9,
                SleepSliceWorld.BedRoom.X, SleepSliceWorld.BedRoom.Y));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            ActorRecord A() => world.Actors.Get(new ActorId(9));

            int tick = 0;
            while (tick < 4 * 60
                   && !(A().ActionState.CurrentAction == ActorActionType.Sleep
                        && A().ActionState.Phase == ActionPhase.Running))
                composer.Advance(world, ++tick);
            Assert.That(A().ActionState.CurrentAction, Is.EqualTo(ActorActionType.Sleep),
                "the horizon must actually reach a Running Sleep before the wake");

            SleepSliceWorld.Interrupt(world, 9UL);
            composer.Advance(world, ++tick);
            // Terminal Failed tick — the verb still tells the truth (the action carrying the failure).
            Assert.That(A().ActionState.CurrentAction, Is.EqualTo(ActorActionType.Sleep),
                "the failure travels ON the Sleep state for the handover tick");
            Assert.That(ActionVerbTable.Verb(A().ActionState.CurrentAction), Is.EqualTo("sleeping"));

            // The handover fires next tick and clears the action; the label goes away with it.
            composer.Advance(world, ++tick);
            Assert.That(A().ActionState.IsIdle || A().ActionState.CurrentAction != ActorActionType.Sleep,
                "handover clears the Sleep — projection can no longer say sleeping");
        }
    }
}
