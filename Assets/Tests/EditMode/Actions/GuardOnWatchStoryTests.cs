using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Simulation.Living.Actions;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W36 GUARD+COMBAT story test #1: OnWatch is the guard's action-strip beat.
    /// PINS: an idle guard walks to DayAnchor and stands post; an armed pursuit interrupts
    /// OnWatch on the very next Advance (matter conservation: no ledger row leaks, the
    /// guard drops to Idle and the existing pursuit lifecycle takes over).
    /// The tests construct ActionLifecycleSystem DIRECTLY with enableGuardAndCombat=true —
    /// the composer wiring is left OFF (protecting the pre-W36 tick surface until goldens
    /// are re-baselined for the flip commit).
    /// </summary>
    public sealed class GuardOnWatchStoryTests
    {
        private static ActionLifecycleSystem Lifecycle()
            => new ActionLifecycleSystem(new ActionLogManager(), enableGuardAndCombat: true);

        // Composer's PerTick 18/22 pair in miniature — the direct-lifecycle idiom the EAT/FARM/SLEEP
        // slice tests use, extended to keep world.Time monotone (workhour gate reads it).
        private static void Pump(WorldState world, ActionLifecycleSystem lifecycle, int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                world.Time = world.Time.AddMinutes(1);
                lifecycle.Decide(world, world.Time);
                lifecycle.Advance(world, world.Time);
            }
        }

        [Test]
        public void OnWatchWalksTheBeatAndCompletes()
        {
            var world = EatSliceWorld.Build(wheat: 0); // no eat rule fires (guard isn't hungry either)
            world.Time = new GameTime(6 * 60); // hour 6 — OnWatch only fires during work hours
            var guard = Guard(id: 7, x: 0, y: 0, dayAnchor: new GridPosition(3, 3));
            world.Actors.Add(guard);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle, 10);

            Assert.That(guard.Position.ChebyshevDistanceTo(new GridPosition(3, 3)),
                Is.LessThanOrEqualTo(OnWatchAdvancer.PostReachCells),
                "guard reached the beat within a few PerTick advances");
            Assert.That(ActionTrace.Of(world), Does.Contain("OnWatch/Running->OnWatch/Succeeded"),
                "the beat completes through the SAME machinery — TransitionTo/Arrived seam");
        }

        [Test]
        public void PursuitInterruptsOnWatchOnNextAdvance()
        {
            var world = EatSliceWorld.Build(wheat: 0);
            world.Time = new GameTime(6 * 60);
            var guard = Guard(id: 7, x: 0, y: 0, dayAnchor: new GridPosition(10, 10)); // far post
            world.Actors.Add(guard);
            var quarry = Bystander(id: 9, x: 12, y: 12);
            world.Actors.Add(quarry);
            var lifecycle = Lifecycle();

            Pump(world, lifecycle); // Decide opens OnWatch, Advance steps one cell in
            Assert.That(guard.ActionState.CurrentIntent, Is.EqualTo(ActorIntent.Watch));
            Assert.That(guard.ActionState.Phase, Is.EqualTo(ActionPhase.Running),
                "guard is still walking to the beat — the interrupt scenario NEEDS a live approach");

            // Arm the pursuit AFTER OnWatch is Running (mid-walk): the "witness arms mid-beat" case.
            world.GuardPursuits.Add(new PursuitRecord
            {
                GuardId = 7, TargetId = 9, UntilMinutes = 1_000_000L,
            });
            Pump(world, lifecycle); // OnWatchAdvancer.Step: HasLivePursuit → Fail(Interrupted)
            Assert.That(guard.ActionState.Phase, Is.EqualTo(ActionPhase.Failed),
                "one Advance drops OnWatch to Failed(Interrupted); the terminal handover is next tick");
            Pump(world, lifecycle); // Failed→Idle terminal handover (Advance's first branch)
            Assert.That(guard.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None),
                "the terminal handover routes Failed→Idle on the next Advance");
            Assert.That(world.HuntTargets.Count, Is.EqualTo(0),
                "matter conservation: guard's OnWatch failure opened NO hunt row");
            var log = ActionTrace.Of(world);
            Assert.That(log, Does.Contain("OnWatch/Running->OnWatch/Failed"),
                "the interruption path went through TransitionTo (single-writer seam preserved)");
        }

        [Test]
        public void GuardOnWatchProjectsAsOnWatchNotAsGuess()
        {
            // W36: the projection reads verb from ActionVerbTable.Verb(OnWatch) verbatim,
            // NOT from the retired "on watch" GUESS row in DescribeScheduleWord.
            Assert.That(ActionVerbTable.Verb(ActorActionType.OnWatch), Is.EqualTo("on watch"),
                "the table row IS the label — retiring DescribeScheduleWord's guess did not lose the string");
            Assert.That(ActionVerbTable.KindName(ActorActionType.OnWatch), Is.EqualTo("OnWatch"),
                "stable KindName for ActorViewState — the pose icon reads this");
        }

        private static ActorRecord Guard(ulong id, int x, int y, GridPosition dayAnchor)
        {
            return new ActorRecord(
                new ActorId(id), "Watch" + id, ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y),
                accuracy: 10, dodge: 5, armor: 0, baseDamage: 1,
                home: new GridPosition(x, y),
                dayAnchor: dayAnchor);
        }

        private static ActorRecord Bystander(ulong id, int x, int y)
        {
            return new ActorRecord(
                new ActorId(id), "Bystander" + id, ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(x, y), accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
        }
    }
}
